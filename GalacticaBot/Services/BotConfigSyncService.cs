using GalacticaBot.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using System.Security.Cryptography.X509Certificates;

namespace GalacticaBot.Services;

public sealed class BotConfigSyncService : BackgroundService
{
    private readonly BotConfigService _configService;
    private readonly GatewayClient _gatewayClient;
    private readonly ILogger<BotConfigSyncService> _logger;
    private readonly PresenceManager _presenceManager;
    private HubConnection? _connection;

    public BotConfigSyncService(
        ILogger<BotConfigSyncService> logger,
        BotConfigService configService,
        PresenceManager presenceManager,
        GatewayClient gatewayClient
    )
    {
        _logger = logger;
        _configService = configService;
        _presenceManager = presenceManager;
        _gatewayClient = gatewayClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var baseUrl = Environment.GetEnvironmentVariable("BOT_API_URL");
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            _logger.LogWarning("BOT_API_URL is not set; BotConfigSyncService will not start.");
            return;
        }

        var hubUrl = CombineUrl(baseUrl, "/hubs/botconfig");

        // Configure HttpClient with client certificate for mTLS
        var certPath = Environment.GetEnvironmentVariable("CERT_PATH") ?? "/app/certs";
        var serviceName = Environment.GetEnvironmentVariable("SERVICE_NAME") ?? "galactica-bot";
        var clientCertPath = Path.Combine(certPath, $"{serviceName}.crt");
        var clientKeyPath = Path.Combine(certPath, $"{serviceName}.key");
        var rootCaPath = Path.Combine(certPath, "root_ca.crt");

        var httpClientHandler = new HttpClientHandler();

        // Configure client certificate if available
        if (File.Exists(clientCertPath) && File.Exists(clientKeyPath))
        {
            _logger.LogInformation("Configuring mTLS with client certificate: {CertPath}", clientCertPath);

            try
            {
                var clientCert = X509Certificate2.CreateFromPemFile(clientCertPath, clientKeyPath);
                httpClientHandler.ClientCertificates.Add(clientCert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load client certificate from {CertPath}", clientCertPath);
                throw;
            }
        }

        // Configure root CA trust if available
        if (File.Exists(rootCaPath))
        {
            _logger.LogInformation("Configuring root CA trust: {RootCaPath}", rootCaPath);

            httpClientHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) =>
            {
                if (cert == null)
                {
                    _logger.LogWarning("Server certificate is null");
                    return false;
                }

                if (chain == null)
                {
                    _logger.LogWarning("Certificate chain is null");
                    return false;
                }

                try
                {
                    using var rootCert = X509Certificate2.CreateFromPemFile(rootCaPath);
                    chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
                    chain.ChainPolicy.CustomTrustStore.Add(rootCert);
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

                    var result = chain.Build(cert);
                    if (!result)
                    {
                        _logger.LogWarning(
                            "Certificate chain validation failed for {Subject}. Errors: {Errors}",
                            cert.Subject,
                            string.Join(", ", chain.ChainStatus.Select(s => s.StatusInformation)));
                    }

                    return result;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error validating server certificate");
                    return false;
                }
            };
        }

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl, options =>
            {
                options.HttpMessageHandlerFactory = _ => httpClientHandler;
            })
            .WithAutomaticReconnect()
            .Build();

        _connection.On(
            "OnConfigurationUpdated",
            async () =>
            {
                _logger.LogInformation("Received configuration update via SignalR.");
                try
                {
                    _configService.InvalidateCache();
                    await _configService.GetCurrentConfigAsync(stoppingToken);
                    await _presenceManager.SetPresence(_gatewayClient);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error handling configuration update notification.");
                }
            }
        );

        while (!stoppingToken.IsCancellationRequested)
            try
            {
                await _connection.StartAsync(stoppingToken);
                _logger.LogInformation("Connected to BotConfigHub at {HubUrl}.", hubUrl);

                // Keep the connection alive
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR connection failed. Retrying in 5 seconds...");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
            try
            {
                await _connection.StopAsync(cancellationToken);
                await _connection.DisposeAsync();
            }
            catch
            {
                // ignore
            }

        await base.StopAsync(cancellationToken);
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        if (baseUrl.EndsWith('/'))
            baseUrl = baseUrl.TrimEnd('/');
        if (!path.StartsWith('/'))
            path = "/" + path;
        return baseUrl + path;
    }
}

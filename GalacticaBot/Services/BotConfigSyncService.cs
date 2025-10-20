using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using GalacticaBot.Utils;

namespace GalacticaBot.Services;

public sealed class BotConfigSyncService : BackgroundService
{
    private readonly ILogger<BotConfigSyncService> _logger;
    private readonly BotConfigService _configService;
    private readonly PresenceManager _presenceManager;
    private readonly GatewayClient _gatewayClient;
    private HubConnection? _connection;

    public BotConfigSyncService(
        ILogger<BotConfigSyncService> logger,
        BotConfigService configService,
        PresenceManager presenceManager,
        GatewayClient gatewayClient)
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

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect()
            .Build();

        _connection.On("OnConfigurationUpdated", async () =>
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
        });

        while (!stoppingToken.IsCancellationRequested)
        {
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
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_connection is not null)
        {
            try
            {
                await _connection.StopAsync(cancellationToken);
                await _connection.DisposeAsync();
            }
            catch
            {
                // ignore
            }
        }
        await base.StopAsync(cancellationToken);
    }

    private static string CombineUrl(string baseUrl, string path)
    {
        if (baseUrl.EndsWith('/')) baseUrl = baseUrl.TrimEnd('/');
        if (!path.StartsWith('/')) path = "/" + path;
        return baseUrl + path;
    }
}

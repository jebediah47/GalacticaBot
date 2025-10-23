using GalacticaBot.Utils;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;

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

        // Get JWT secret for hub authentication
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET");
        if (string.IsNullOrWhiteSpace(jwtSecret) || jwtSecret.Length < 32)
        {
            _logger.LogWarning(
                "JWT_SECRET is not set or is less than 32 characters; "
                    + "BotConfigSyncService will not be able to authenticate with the hub."
            );
            return;
        }

        var botId = Environment.GetEnvironmentVariable("BOT_ID") ?? "galactica-bot";
        var hubUrl = CombineUrl(baseUrl, "/hubs/botconfig");

        // Generate JWT token for hub authentication
        var token = JwtTokenGenerator.GenerateToken(jwtSecret, botId);
        var hubUrlWithAuth = $"{hubUrl}?access_token={Uri.EscapeDataString(token)}";

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrlWithAuth)
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

                // Refresh token every 50 minutes (token expires in 60 minutes)
                _ = RefreshTokenPeriodicallyAsync(hubUrl, jwtSecret, botId, stoppingToken);

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

    /// <summary>
    /// Periodically refreshes the JWT token and reconnects to the hub before token expiration.
    /// </summary>
    private async Task RefreshTokenPeriodicallyAsync(
        string baseHubUrl,
        string jwtSecret,
        string botId,
        CancellationToken cancellationToken
    )
    {
        try
        {
            // Refresh every 50 minutes (token expires in 60 minutes)
            await Task.Delay(TimeSpan.FromMinutes(50), cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (_connection?.State == HubConnectionState.Connected)
                    {
                        _logger.LogInformation("Refreshing JWT token for BotConfigHub.");

                        // Stop existing connection
                        await _connection.StopAsync(cancellationToken);

                        // Generate new token
                        var newToken = JwtTokenGenerator.GenerateToken(jwtSecret, botId);
                        var hubUrlWithAuth =
                            $"{baseHubUrl}?access_token={Uri.EscapeDataString(newToken)}";

                        // Recreate connection with new token
                        _connection = new HubConnectionBuilder()
                            .WithUrl(hubUrlWithAuth)
                            .WithAutomaticReconnect()
                            .Build();

                        _connection.On(
                            "OnConfigurationUpdated",
                            async () =>
                            {
                                _logger.LogInformation(
                                    "Received configuration update via SignalR."
                                );
                                try
                                {
                                    _configService.InvalidateCache();
                                    await _configService.GetCurrentConfigAsync(cancellationToken);
                                    await _presenceManager.SetPresence(_gatewayClient);
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogError(
                                        ex,
                                        "Error handling configuration update notification."
                                    );
                                }
                            }
                        );

                        await _connection.StartAsync(cancellationToken);
                        _logger.LogInformation("Reconnected to BotConfigHub with refreshed token.");
                    }

                    // Wait 50 minutes before next refresh
                    await Task.Delay(TimeSpan.FromMinutes(50), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error refreshing token. Will retry in 5 minutes.");
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when service stops
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

using GalacticaBot.Utils;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace GalacticaBot.Events.Client;

public sealed class ReadyHandler(
    ILogger<ReadyHandler> logger,
    GatewayClient gatewayClient,
    PresenceManager presenceManager
) : IReadyGatewayHandler
{
    public async ValueTask HandleAsync(ReadyEventArgs arg)
    {
        logger.LogInformation($"We have logged in as {arg.User.Username}");
        await presenceManager.SetPresence(gatewayClient);
    }
}

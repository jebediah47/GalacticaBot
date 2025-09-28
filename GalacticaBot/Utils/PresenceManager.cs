using GalacticaBot.Configuration;
using NetCord.Gateway;

namespace GalacticaBot.Utils;

public sealed class PresenceManager(IBotConfig botConfig)
{
    public async ValueTask SetPresence(GatewayClient gatewayClient)
    {
        var presence = new PresenceProperties(botConfig.BotStatus)
        {
            Activities = [new UserActivityProperties(botConfig.BotPresence, botConfig.BotActivity)],
        };

        await gatewayClient.UpdatePresenceAsync(presence);
    }
}

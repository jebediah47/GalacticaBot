using GalacticaBot.Services;
using NetCord.Gateway;

namespace GalacticaBot.Utils;

public sealed class PresenceManager
{
    private readonly BotConfigService _configService;

    public PresenceManager(BotConfigService configService)
    {
        _configService = configService;
    }

    public async ValueTask SetPresence(GatewayClient gatewayClient)
    {
        var config = await _configService.GetCurrentConfigAsync();

        var presence = new PresenceProperties(config.BotStatus)
        {
            Activities = [new UserActivityProperties(config.BotPresence, config.BotActivity)],
        };

        await gatewayClient.UpdatePresenceAsync(presence);
    }
}

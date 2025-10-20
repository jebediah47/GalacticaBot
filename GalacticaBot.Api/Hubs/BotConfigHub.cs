using Microsoft.AspNetCore.SignalR;

namespace GalacticaBot.Api.Hubs;

public sealed class BotConfigHub : Hub
{
    public async Task NotifyConfigUpdated()
    {
        await Clients.All.SendAsync("OnConfigurationUpdated");
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace GalacticaBot.Api.Hubs;

[Authorize(Policy = "SignalRHub")]
public sealed class BotConfigHub : Hub
{
    public async Task NotifyConfigUpdated()
    {
        await Clients.All.SendAsync("OnConfigurationUpdated");
    }
}

using Microsoft.AspNetCore.SignalR;

namespace GalacticaBot.Api.Hubs;

public sealed class GuildConfigHub : Hub
{
    public async Task JoinGuildGroup(ulong guildId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"guild-{guildId}");
    }

    public async Task LeaveGuildGroup(ulong guildId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"guild-{guildId}");
    }
}

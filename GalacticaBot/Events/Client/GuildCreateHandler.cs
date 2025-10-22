using GalacticaBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace GalacticaBot.Events.Client;

public sealed class GuildCreateHandler(
    ILogger<GuildCreateHandler> logger,
    IDbContextFactory<GalacticaDbContext> dbFactory
) : IGuildCreateGatewayHandler
{
    public async ValueTask HandleAsync(GuildCreateEventArgs arg)
    {
        var guild = arg.Guild!;
        await using var db = await dbFactory.CreateDbContextAsync();
        if (await db.GuildConfigs.AnyAsync(c => c.GuildId == guild.Id))
            return;

        var newConfig = new GuildConfigs
        {
            GuildId = guild.Id,
            GuildName = guild.Name,
            DateJoined = DateTime.UtcNow,
            ModLogsIsEnabled = false,
            ModLogsChannelId = null,
            LastUpdated = DateTime.UtcNow,
        };

        db.GuildConfigs.Add(newConfig);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "Added new guild config for guild {GuildId} ({GuildName})",
            guild.Id,
            guild.Name
        );
    }
}

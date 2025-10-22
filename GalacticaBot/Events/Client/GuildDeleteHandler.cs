using GalacticaBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace GalacticaBot.Events.Client;

public sealed class GuildDeleteHandler(
    ILogger<GuildDeleteHandler> logger,
    IDbContextFactory<GalacticaDbContext> dbFactory
) : IGuildDeleteGatewayHandler
{
    public async ValueTask HandleAsync(GuildDeleteEventArgs arg)
    {
        var guildId = arg.GuildId;
        await using var db = await dbFactory.CreateDbContextAsync();
        // GuildConfigs has int PK (Id) and a separate ulong GuildId property.
        // Don't call FindAsync with the ulong; query by the GuildId property instead.
        var existingConfig = await db.GuildConfigs.FirstOrDefaultAsync(c => c.GuildId == guildId);
        if (existingConfig is null)
            return;

        db.GuildConfigs.Remove(existingConfig);
        await db.SaveChangesAsync();

        logger.LogInformation("Removed guild config for guild {GuildId}", guildId);
    }
}

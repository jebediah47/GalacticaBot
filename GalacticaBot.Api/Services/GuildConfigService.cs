using GalacticaBot.Api.Hubs;
using GalacticaBot.Api.Models;
using GalacticaBot.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace GalacticaBot.Api.Services;

public class GuildConfigService(
    IDbContextFactory<GalacticaDbContext> dbFactory,
    IHubContext<GuildConfigHub> hubContext
)
{
    public async Task<GuildConfigDto> UpdateModLogsAsync(
        ulong guildId,
        GuildConfigUpdateDto dto,
        CancellationToken ct = default
    )
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var entity = await db.GuildConfigs.FirstOrDefaultAsync(x => x.GuildId == guildId, ct);
        if (entity is null)
        {
            throw new KeyNotFoundException($"Guild config for guild {guildId} not found.");
        }

        // Update fields
        entity.ModLogsIsEnabled = dto.ModLogsIsEnabled;
        entity.ModLogsChannelId = dto.ModLogsChannelId;
        entity.LastUpdated = DateTime.UtcNow;

        // Use transaction
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            await db.SaveChangesAsync(ct);

            await transaction.CommitAsync(ct);

            // Broadcast
            var dtoResponse = GuildConfigDto.FromEntity(entity);
            await hubContext
                .Clients.Group($"guild-{guildId}")
                .SendAsync("GuildConfigUpdated", dtoResponse, ct);

            return dtoResponse;
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(ct);
            throw new InvalidOperationException("Concurrency conflict occurred.");
        }
        catch
        {
            await transaction.RollbackAsync(ct);
            throw;
        }
    }
}

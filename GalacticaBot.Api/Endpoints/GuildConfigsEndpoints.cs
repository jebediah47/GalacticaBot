using GalacticaBot.Api.Models;
using GalacticaBot.Api.Services;
using GalacticaBot.Data;
using Microsoft.EntityFrameworkCore;

namespace GalacticaBot.Api.Endpoints;

public static class GuildConfigsEndpoints
{
    public static void MapGuildConfigsEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/guilds");

        api.MapGet(
                "/{guildId}/modlogs",
                async (
                    ulong guildId,
                    IDbContextFactory<GalacticaDbContext> dbFactory,
                    CancellationToken ct
                ) =>
                {
                    await using var db = await dbFactory.CreateDbContextAsync(ct);
                    var entity = await db.GuildConfigs.FirstOrDefaultAsync(
                        x => x.GuildId == guildId,
                        ct
                    );
                    if (entity is null)
                    {
                        return Results.NotFound();
                    }
                    return Results.Ok(GuildConfigDto.FromEntity(entity));
                }
            )
            .WithName("GetGuildModLogs")
            .WithOpenApi();

        api.MapPut(
                "/{guildId}/modlogs",
                async (
                    ulong guildId,
                    GuildConfigUpdateDto dto,
                    GuildConfigService service,
                    CancellationToken ct
                ) =>
                {
                    try
                    {
                        var result = await service.UpdateModLogsAsync(guildId, dto, ct);
                        return Results.Ok(result);
                    }
                    catch (KeyNotFoundException)
                    {
                        return Results.NotFound();
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("Concurrency"))
                    {
                        return Results.Conflict("Concurrency conflict.");
                    }
                    catch (Exception)
                    {
                        // Log ex
                        return Results.Problem("Internal server error.");
                    }
                }
            )
            .WithName("UpdateGuildModLogs")
            .WithOpenApi();
    }
}

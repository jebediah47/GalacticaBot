using GalacticaBot.Api.Hubs;
using GalacticaBot.Api.Models;
using GalacticaBot.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Gateway;

namespace GalacticaBot.Api.Endpoints;

public static class BotConfigEndpoints
{
    public static void MapBotConfigEndpoints(this WebApplication app)
    {
        var api = app.MapGroup("/api/botconfig");

        api.MapGet(
            "/",
            async (IDbContextFactory<GalacticaDbContext> dbFactory, CancellationToken ct) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var entity = await db.BotConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
                if (entity is null)
                {
                    entity = new BotConfig
                    {
                        BotStatus = UserStatusType.Online,
                        BotPresence = "Ready",
                        BotActivity = UserActivityType.Playing,
                        LastUpdated = DateTime.UtcNow,
                    };
                    db.BotConfigs.Add(entity);
                    await db.SaveChangesAsync(ct);
                }

                return Results.Ok(BotConfigDto.FromEntity(entity));
            }
        );

        api.MapPost(
            "/",
            async (
                BotConfigDto request,
                IDbContextFactory<GalacticaDbContext> dbFactory,
                IHubContext<BotConfigHub> hub,
                CancellationToken ct
            ) =>
            {
                await using var db = await dbFactory.CreateDbContextAsync(ct);
                var entity = await db.BotConfigs.SingleOrDefaultAsync(ct);
                if (entity is null)
                {
                    entity = new BotConfig { Id = 1 };
                    db.BotConfigs.Add(entity);
                }

                entity.BotStatus = request.BotStatus;
                entity.BotPresence = request.BotPresence;
                entity.BotActivity = request.BotActivity;
                entity.LastUpdated = DateTime.UtcNow;

                await db.SaveChangesAsync(ct);

                // Notify all clients
                await hub.Clients.All.SendAsync("OnConfigurationUpdated", ct);

                return Results.Ok(BotConfigDto.FromEntity(entity));
            }
        );
    }
}

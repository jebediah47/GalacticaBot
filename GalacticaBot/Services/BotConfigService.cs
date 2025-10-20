using GalacticaBot.Data;
using Microsoft.EntityFrameworkCore;
using NetCord;
using NetCord.Gateway;

namespace GalacticaBot.Services;

public sealed class BotConfigService
{
    private readonly IDbContextFactory<GalacticaDbContext> _dbFactory;
    private BotConfig? _cache;
    private readonly SemaphoreSlim _sync = new(1, 1);

    public BotConfigService(IDbContextFactory<GalacticaDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<BotConfig> GetCurrentConfigAsync(CancellationToken ct = default)
    {
        if (_cache is not null)
            return _cache;

        await _sync.WaitAsync(ct);
        try
        {
            if (_cache is not null)
                return _cache;

            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var config = await db.BotConfigs.AsNoTracking().SingleOrDefaultAsync(ct);
            if (config is null)
            {
                // Create default row with id = 1
                config = new BotConfig
                {
                    BotStatus = UserStatusType.Online,
                    BotPresence = "Ready",
                    BotActivity = UserActivityType.Playing,
                    LastUpdated = DateTime.UtcNow,
                };
                db.BotConfigs.Add(config);
                await db.SaveChangesAsync(ct);
            }

            _cache = config;
            return config;
        }
        finally
        {
            _sync.Release();
        }
    }

    public async Task<BotConfig> UpdateConfigAsync(
        BotConfig newConfig,
        CancellationToken ct = default
    )
    {
        await _sync.WaitAsync(ct);
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(ct);

            var existing = await db.BotConfigs.SingleOrDefaultAsync(ct);
            if (existing is null)
            {
                newConfig.Id = 1; // enforce single-row rule
                newConfig.LastUpdated = DateTime.UtcNow;
                db.BotConfigs.Add(newConfig);
                await db.SaveChangesAsync(ct);
                _cache = newConfig;
                return newConfig;
            }

            existing.BotStatus = newConfig.BotStatus;
            existing.BotPresence = newConfig.BotPresence;
            existing.BotActivity = newConfig.BotActivity;
            existing.LastUpdated = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            // Update cache with fresh copy
            _cache = existing;
            return existing;
        }
        finally
        {
            _sync.Release();
        }
    }

    public void InvalidateCache()
    {
        _cache = null;
    }
}

using GalacticaBot.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GalacticaBot.Services;

public sealed class LevelingService
{
    private readonly IDbContextFactory<GalacticaDbContext> _dbFactory;
    private readonly ILogger<LevelingService> _logger;

    public LevelingService(
        IDbContextFactory<GalacticaDbContext> dbFactory,
        ILogger<LevelingService> logger
    )
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public readonly record struct LevelData(int Level, int Xp);

    public readonly record struct LevelUpResult(bool LeveledUp, int NewLevel);

    public int GetRandomXp(string messageContent)
    {
        if (string.IsNullOrEmpty(messageContent))
            return 0;
        var msgLength = messageContent.Length;
        var xp = Math.Floor(msgLength / 2.0);
        return (int)Math.Min(xp, 50);
    }

    /// <summary>
    /// Awards XP for a user in a guild. Creates the record if missing. Returns whether a level-up happened.
    /// </summary>
    public async Task<LevelUpResult> GiveXpAsync(string userId, string guildId, int xpToGive)
    {
        if (xpToGive <= 0)
            return new LevelUpResult(false, 0);

        await using var db = await _dbFactory.CreateDbContextAsync();

        // Try fetch existing
        var entry = await db.LevelModels.FirstOrDefaultAsync(e =>
            e.UserID == userId && e.GuildID == guildId
        );
        if (entry is null)
        {
            // Attempt to insert new record
            entry = new LevelModel
            {
                UserID = userId,
                GuildID = guildId,
                Xp = xpToGive,
                Level = 0,
                LastXpMsg = DateTime.UtcNow,
            };
            db.LevelModels.Add(entry);
            try
            {
                await db.SaveChangesAsync();
                return new LevelUpResult(false, 0);
            }
            catch (DbUpdateException ex)
            {
                // Likely a race: record already created by another concurrent request. Log and fall through to update path.
                _logger.LogDebug(
                    ex,
                    "Concurrent insert detected for ({UserId}, {GuildId}). Retrying as update.",
                    userId,
                    guildId
                );
                db.ChangeTracker.Clear();
                entry = await db.LevelModels.FirstOrDefaultAsync(e =>
                    e.UserID == userId && e.GuildID == guildId
                );
                if (entry is null)
                {
                    // Give up silently if still not found
                    return new LevelUpResult(false, 0);
                }
            }
        }

        // Update existing
        var newXp = entry.Xp + xpToGive;
        var userLvl = entry.Level;

        // Linear scaling: 5 * level^2 + 50 * level + 100
        // Level 0→1: 100, Level 1→2: 155, Level 2→3: 220, Level 5→6: 475, Level 10→11: 1,100, Level 20→21: 3,100
        var xpToLevelUp = 5 * userLvl * userLvl + 50 * userLvl + 100;

        var leveledUp = false;
        if (newXp >= xpToLevelUp)
        {
            userLvl += 1;
            leveledUp = true;
        }

        entry.Xp = newXp;
        entry.Level = userLvl;
        entry.LastXpMsg = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return new LevelUpResult(leveledUp, userLvl);
    }

    private async Task<LevelModel?> FindXpUserAsync(string userId, string guildId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        return await db
            .LevelModels.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserID == userId && e.GuildID == guildId);
    }

    private async Task<int?> GetUserLevelAsync(string userId, string guildId)
    {
        var entry = await FindXpUserAsync(userId, guildId);
        return entry?.Level;
    }

    private async Task<int?> GetUserXpAsync(string userId, string guildId)
    {
        var entry = await FindXpUserAsync(userId, guildId);
        if (entry is null)
            return null;
        // Clamp to int range for the API surface
        return entry.Xp switch
        {
            > int.MaxValue => int.MaxValue,
            < int.MinValue => int.MinValue,
            _ => (int)entry.Xp,
        };
    }

    // Accepts userId and guildId instead of an interaction, per request
    public async Task<LevelData> GetUserStatsAsync(string userId, string guildId)
    {
        var level = await GetUserLevelAsync(userId, guildId);
        var xp = await GetUserXpAsync(userId, guildId);

        // Mirror JS behavior where Number(null) -> 0
        return new LevelData(level ?? 0, xp ?? 0);
    }
}

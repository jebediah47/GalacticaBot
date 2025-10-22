using GalacticaBot.Data;

namespace GalacticaBot.Api.Models;

public class GuildConfigDto
{
    public ulong GuildId { get; set; }
    public bool ModLogsIsEnabled { get; set; }
    public ulong? ModLogsChannelId { get; set; }
    public DateTime LastUpdated { get; set; }

    public static GuildConfigDto FromEntity(GuildConfigs entity) =>
        new()
        {
            GuildId = entity.GuildId,
            ModLogsIsEnabled = entity.ModLogsIsEnabled,
            ModLogsChannelId = entity.ModLogsChannelId,
            LastUpdated = entity.LastUpdated,
        };
}

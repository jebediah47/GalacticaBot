using System;

namespace GalacticaBot.Data;

public sealed class GuildConfigs
{
    public int Id { get; set; }
    public string GuildID { get; set; } = null!;
    public string GuildName { get; set; } = null!;
    public DateTime DateJoined { get; set; }
    public bool ModLogsIsEnabled { get; set; }
    public string? ModLogsChannelID { get; set; }
}

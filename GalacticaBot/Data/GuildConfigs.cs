namespace GalacticaBot.Data;

public sealed class GuildConfigs
{
    public int Id { get; set; }
    public ulong GuildID { get; set; }
    public string GuildName { get; set; } = null!;
    public DateTime DateJoined { get; set; }
    public bool ModLogsIsEnabled { get; set; }
    public string? ModLogsChannelID { get; set; }
}

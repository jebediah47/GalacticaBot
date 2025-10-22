namespace GalacticaBot.Data;

public sealed class GuildConfigs
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public string GuildName { get; set; } = null!;
    public DateTime DateJoined { get; set; }
    public bool ModLogsIsEnabled { get; set; }
    public ulong? ModLogsChannelId { get; set; }
    public DateTime LastUpdated { get; set; }
    public uint RowVersion { get; set; }
}

namespace GalacticaBot.Data;

public sealed class LevelModel
{
    public string Id { get; set; } = null!;
    public ulong UserID { get; set; }
    public ulong GuildID { get; set; }
    public long Xp { get; set; }
    public int Level { get; set; }
    public DateTime LastXpMsg { get; set; }
}

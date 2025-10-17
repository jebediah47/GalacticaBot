using System;

namespace GalacticaBot.Data;

public class LevelModel
{
    public string Id { get; set; } = null!;
    public string UserID { get; set; } = null!;
    public string GuildID { get; set; } = null!;
    public long Xp { get; set; }
    public int Level { get; set; }
    public DateTime LastXpMsg { get; set; }
}

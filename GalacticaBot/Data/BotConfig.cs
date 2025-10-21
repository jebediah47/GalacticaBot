using NetCord;
using NetCord.Gateway;

namespace GalacticaBot.Data;

public sealed class BotConfig
{
    public int Id { get; set; }
    public UserStatusType BotStatus { get; set; }
    public string BotPresence { get; set; } = string.Empty;
    public UserActivityType BotActivity { get; set; }
    public DateTime LastUpdated { get; set; }
}

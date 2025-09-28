using NetCord;
using NetCord.Gateway;

namespace GalacticaBot.Configuration;

public interface IBotConfig
{
    UserStatusType BotStatus { get; set; }
    string BotPresence { get; set; }
    UserActivityType BotActivity { get; set; }
}

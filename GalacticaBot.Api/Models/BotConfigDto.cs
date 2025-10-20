using System.ComponentModel.DataAnnotations;
using GalacticaBot.Data;
using NetCord;
using NetCord.Gateway;

namespace GalacticaBot.Api.Models;

public sealed class BotConfigDto
{
    public int? Id { get; init; }

    [Required]
    public UserStatusType BotStatus { get; init; }

    [Required]
    [StringLength(256, MinimumLength = 0)]
    public string BotPresence { get; init; } = string.Empty;

    [Required]
    public UserActivityType BotActivity { get; init; }

    public DateTime? LastUpdated { get; init; }

    public static BotConfigDto FromEntity(BotConfig e) =>
        new()
        {
            Id = e.Id,
            BotStatus = e.BotStatus,
            BotPresence = e.BotPresence,
            BotActivity = e.BotActivity,
            LastUpdated = e.LastUpdated,
        };
}

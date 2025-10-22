namespace GalacticaBot.Api.Models;

public class GuildConfigUpdateDto
{
    public bool ModLogsIsEnabled { get; set; }
    public ulong? ModLogsChannelId { get; set; }
}

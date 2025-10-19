using GalacticaBot.Services;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;

namespace GalacticaBot.Events.Client;

public sealed class MessageCreateHandler(
    ILogger<MessageCreateHandler> logger,
    LevelingService leveling
) : IMessageCreateGatewayHandler
{
    public async ValueTask HandleAsync(Message msg)
    {
        // Ignore non-guild messages and bot authors
        if (msg.GuildId is null)
            return;
        if (msg.Author.IsBot)
            return;

        var content = msg.Content;
        var xp = leveling.GetRandomXp(content);
        if (xp <= 0)
            return;

        var userId = msg.Author.Id;
        var guildId = msg.GuildId.Value;

        var result = await leveling.GiveXpAsync(userId, guildId, xp);
        if (result.LeveledUp)
        {
            logger.LogInformation(
                "User {UserId} leveled up to {Level} in guild {GuildId} (channel {ChannelId})",
                userId,
                result.NewLevel,
                guildId,
                msg.ChannelId
            );

            // Reply immediately to the triggering message
            try
            {
                await msg.ReplyAsync($"You've leveled up to level {result.NewLevel}!");
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to send level-up reply for user {UserId} in guild {GuildId}",
                    userId,
                    guildId
                );
            }
        }
    }
}

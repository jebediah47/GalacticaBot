using GalacticaBot.Services;
using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Leveling;

public sealed class Rank(LevelingService levelingService)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand(
        "rank",
        "Shows your level and XP",
        DefaultGuildPermissions = Permissions.SendMessages,
        Contexts = [InteractionContextType.Guild]
    )]
    public async Task Run()
    {
        var ctx = Context;
        var userLevelData = await levelingService.GetUserStatsAsync(ctx.User.Id, ctx.Guild!.Id);

        if (userLevelData.Xp < 25)
        {
            await RespondAsync(
                InteractionCallback.Message("You don't have enough XP to run this command!")
            );
        }

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Get())
            .WithAuthor(
                new EmbedAuthorProperties
                {
                    Name = ctx.User.Username,
                    IconUrl = ctx.User.GetAvatarUrl(ImageFormat.Png)?.ToString(),
                }
            )
            .WithFields(
                [
                    new EmbedFieldProperties
                    {
                        Name = "Level",
                        Value = $"```{userLevelData.Level.ToString()}```",
                        Inline = true,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "XP",
                        Value = $"```{userLevelData.Xp.ToString()}```",
                        Inline = true,
                    },
                ]
            )
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }
}

using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Fun;

public sealed class Avatar(IRandomColor randomColor)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("avatar", "Displays the mentioned user's avatar")]
    public Task Run(
        [SlashCommandParameter(
            Name = "user",
            Description = "Mention any user for his avatar to be displayed"
        )]
            User? user = null
    )
    {
        user ??= Context.User;

        var avatarEmbed = new EmbedProperties()
            .WithColor(randomColor.GetRandomColor())
            .WithTitle($"{user.Username}'s avatar")
            .WithImage(new EmbedImageProperties(user.GetAvatarUrl(ImageFormat.Png) + "?size=4096"))
            .WithTimestamp(DateTimeOffset.UtcNow);

        return RespondAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties().WithEmbeds([avatarEmbed])
            )
        );
    }
}

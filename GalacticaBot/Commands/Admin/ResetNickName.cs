using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Admin;

public sealed class ResetNickName : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand(
        "resetnickname",
        "Resets the nickname of the mentioned user back to their username",
        DefaultGuildPermissions = Permissions.ChangeNickname,
        Contexts = [InteractionContextType.Guild]
    )]
    public async Task Run(
        [SlashCommandParameter(Name = "user", Description = "The user's nickname to reset")]
            GuildUser user
    )
    {
        await user.ModifyAsync(x => x.Nickname = user.Username);

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Generate())
            .WithTitle("✅ Nickname changed!")
            .WithDescription($"**{user.Username}**'s nickname has been reset")
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }
}

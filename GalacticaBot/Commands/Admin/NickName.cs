using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Admin;

public sealed class NickName : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand(
        "nickname",
        "Changes the nickname of the mentioned user",
        DefaultGuildPermissions = Permissions.ChangeNickname,
        Contexts = [InteractionContextType.Guild]
    )]
    public async Task Run(
        [SlashCommandParameter(Name = "user", Description = "The user's nickname to change")]
            GuildUser user,
        [SlashCommandParameter(Name = "nickname", Description = "The new nickname")] string nickname
    )
    {
        await user.ModifyAsync(x => x.Nickname = nickname);

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Get())
            .WithTitle("✅ Nickname changed!")
            .WithDescription($"**{user.Username}**'s nickname has been changed to **{nickname}**")
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }
}

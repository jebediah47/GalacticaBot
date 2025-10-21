using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Admin;

public sealed class UnlockChannel : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand(
        "unlock",
        "Unlocks the current or mentioned channel",
        DefaultGuildPermissions = Permissions.ManageChannels,
        Contexts = [InteractionContextType.Guild]
    )]
    public async Task Run(
        [SlashCommandParameter(Name = "channel", Description = "The channel to unlock")]
            TextGuildChannel? channel = null
    )
    {
        var everyone = Context.Guild?.EveryoneRole;

        if (everyone is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [
                            GlobalErrorEmbed.Generate(
                                "Could not find `@everyone` role (guild might be partial)"
                            ),
                        ]
                    )
                )
            );
            return;
        }

        channel ??= Context.Channel as TextGuildChannel;

        if (channel is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Generate("Invalid channel")]
                    )
                )
            );
            return;
        }

        if (everyone.Permissions.HasFlag(Permissions.SendMessages))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Generate($"Channel `{channel.Name}` is already unlocked")]
                    )
                )
            );
            return;
        }

        await channel.ModifyPermissionsAsync(
            new PermissionOverwriteProperties(everyone.Id, PermissionOverwriteType.Role)
            {
                Allowed = Permissions.SendMessages,
            }
        );

        await RespondAsync(InteractionCallback.Message($"🔓 Unlocked `{channel.Name}` channel"));
    }
}

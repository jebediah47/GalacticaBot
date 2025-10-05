using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Admin;

public sealed class LockChannel : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand(
        "lock",
        "Locks the current or mentioned channel",
        DefaultGuildPermissions = Permissions.ManageChannels,
        Contexts = [InteractionContextType.Guild]
    )]
    public async Task Run(
        [SlashCommandParameter(Name = "channel", Description = "The channel to lock")]
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
                            GlobalErrorEmbed.Get(
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
                        [GlobalErrorEmbed.Get("Invalid channel")]
                    )
                )
            );
            return;
        }

        if (!everyone.Permissions.HasFlag(Permissions.SendMessages))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get($"Channel `{channel.Name}` is already locked")]
                    )
                )
            );
            return;
        }

        await channel.ModifyPermissionsAsync(
            new PermissionOverwriteProperties(everyone.Id, PermissionOverwriteType.Role)
            {
                Denied = Permissions.SendMessages,
            }
        );

        await RespondAsync(InteractionCallback.Message($"🔒 Locked `{channel.Name}` channel"));
    }
}

using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Admin;

public sealed class SlowMode : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand(
        "slowmode",
        "Sets slowmode for the channel",
        DefaultGuildPermissions = Permissions.ManageChannels,
        Contexts = [InteractionContextType.Guild]
    )]
    public async Task Run(
        [SlashCommandParameter(
            Name = "time",
            Description = "Time in seconds (0 to disable)",
            MinValue = 0,
            MaxValue = 21600
        )]
            int time,
        [SlashCommandParameter(Name = "channel", Description = "The channel to set slowmode for")]
            TextGuildChannel? channel = null
    )
    {
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

        await channel.ModifyAsync(options => options.Slowmode = time);

        await RespondAsync(
            InteractionCallback.Message($"⏱️ Set slowmode to {time} seconds in `{channel.Name}`")
        );
    }
}

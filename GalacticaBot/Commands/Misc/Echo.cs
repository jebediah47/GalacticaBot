using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Misc;

public sealed class Echo : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("echo", "Echoes your message")]
    public Task Run(
        [SlashCommandParameter(Name = "message", Description = "The message to echo")]
            string message
    )
    {
        return RespondAsync(InteractionCallback.Message(message));
    }
}

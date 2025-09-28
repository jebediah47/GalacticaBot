using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Misc;

public sealed class Ping : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ping", "Pings the bot")]
    public Task Run()
    {
        return RespondAsync(InteractionCallback.Message("Pong!"));
    }
}

using Figgle.Fonts;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Fun;

public sealed class Ascii : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ascii", "Converts text to ASCII art")]
    public Task Run(
        [SlashCommandParameter(Name = "text", Description = "The text to convert", MaxLength = 20)]
            string text
    )
    {
        var asciiArt = FiggleFonts.Standard.Render(text);

        return RespondAsync(InteractionCallback.Message($"```{asciiArt}```"));
    }
}

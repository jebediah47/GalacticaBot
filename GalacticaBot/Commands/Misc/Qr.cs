using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Misc;

public sealed class Qr : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("qr", "Generates a QR code based on the provided text")]
    public async Task Run(
        [SlashCommandParameter(Name = "text", Description = "The text to encode into a QR code")]
            string text
    )
    {
        var interaction = Context.Interaction;
        await interaction.SendResponseAsync(
            InteractionCallback.Message("Please wait while your text is converted to a QR-code")
        );

        var encodedText = Uri.EscapeDataString(text);

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Generate())
            .WithImage(
                $"https://api.qrserver.com/v1/create-qr-code/?size=150x150&data=${encodedText}"
            )
            .WithTimestamp(DateTimeOffset.UtcNow);

        await interaction.ModifyResponseAsync(msg =>
        {
            msg.Content = string.Empty;
            msg.Embeds = [embed];
        });
    }
}

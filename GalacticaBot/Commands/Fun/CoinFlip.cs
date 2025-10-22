namespace GalacticaBot.Commands.Fun;

using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

public sealed class CoinFlip : ApplicationCommandModule<ApplicationCommandContext>
{
    // Public domain / Wikimedia images for heads/tails (used directly as embed images)
    private static readonly string[] CoinImages =
    [
        "https://upload.wikimedia.org/wikipedia/commons/2/28/98_quarter_obverse.png", // heads
        "https://upload.wikimedia.org/wikipedia/commons/5/5a/98_quarter_reverse.png", // tails
    ];

    private static readonly string[] CoinNames = new[] { "Heads", "Tails" };

    [SlashCommand("coinflip", "Flips a coin and returns heads or tails")]
    public async Task Run()
    {
        var rng = new Random();
        var idx = rng.Next(0, 2);

        var embed = new EmbedProperties()
            .WithTitle($"You got {CoinNames[idx]}!")
            .WithImage(new EmbedImageProperties(CoinImages[idx]))
            .WithColor(RandomColor.Get())
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }
}

using NetCord.Rest;

namespace GalacticaBot.Utils;

public static class GlobalErrorEmbed
{
    public static EmbedProperties Get(string? msg = null)
    {
        msg ??=
            "An unexpected error has occured and we're working hard to fix it, sorry for the inconvenience.";

        return new EmbedProperties()
            .WithColor(RandomColor.Get())
            .WithTitle("❌ An unexpected error occured!")
            .WithDescription(msg)
            .WithTimestamp(DateTimeOffset.UtcNow);
    }
}

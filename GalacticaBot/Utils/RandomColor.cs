using NetCord;

namespace GalacticaBot.Utils;

public static class RandomColor
{
    private static readonly Random Rnd = new Random();

    public static Color Get()
    {
        return new Color((byte)Rnd.Next(256), (byte)Rnd.Next(256), (byte)Rnd.Next(256));
    }
}

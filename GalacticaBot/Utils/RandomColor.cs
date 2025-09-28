using NetCord;

namespace GalacticaBot.Utils;

public interface IRandomColor
{
    Color GetRandomColor();
}

public sealed class RandomColor : IRandomColor
{
    private readonly Random _rnd = new Random();

    public Color GetRandomColor()
    {
        return new Color((byte)_rnd.Next(256), (byte)_rnd.Next(256), (byte)_rnd.Next(256));
    }
}

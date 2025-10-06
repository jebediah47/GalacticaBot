using System.Text.Json.Serialization;

namespace GalacticaBot.Models;

public sealed class BoredActivityModel
{
    [JsonPropertyName("activity")]
    public string Activity { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("participants")]
    public int Participants { get; set; }

    [JsonPropertyName("price")]
    public double Price { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; } = string.Empty;

    [JsonPropertyName("key")]
    public string Key { get; set; } = string.Empty;

    [JsonPropertyName("accessibility")]
    public string Accessibility { get; set; } = string.Empty;

    [JsonPropertyName("availability")]
    public double Availability { get; set; }

    [JsonPropertyName("duration")]
    public string Duration { get; set; } = string.Empty;

    [JsonPropertyName("kidFriendly")]
    public bool KidFriendly { get; set; }
}

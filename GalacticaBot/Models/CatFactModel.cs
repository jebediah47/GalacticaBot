using System.Text.Json.Serialization;

namespace GalacticaBot.Models;

public sealed class CatFactModel
{
    [JsonPropertyName("fact")]
    public string Fact { get; set; } = string.Empty;

    [JsonPropertyName("length")]
    public int Length { get; set; }
}

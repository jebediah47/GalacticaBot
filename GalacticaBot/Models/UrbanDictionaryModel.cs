using System.Text.Json.Serialization;

namespace GalacticaBot.Models;

public sealed class UrbanDictionaryModel
{
    [JsonPropertyName("list")]
    public List<UrbanDictionaryDefinition> List { get; set; } = [];
}

public sealed class UrbanDictionaryDefinition
{
    [JsonPropertyName("author")]
    public string Author { get; set; } = string.Empty;

    [JsonPropertyName("current_vote")]
    public string CurrentVote { get; set; } = string.Empty;

    [JsonPropertyName("defid")]
    public long DefId { get; set; }

    [JsonPropertyName("definition")]
    public string Definition { get; set; } = string.Empty;

    [JsonPropertyName("example")]
    public string Example { get; set; } = string.Empty;

    [JsonPropertyName("permalink")]
    public string Permalink { get; set; } = string.Empty;

    [JsonPropertyName("thumbs_down")]
    public int ThumbsDown { get; set; }

    [JsonPropertyName("thumbs_up")]
    public int ThumbsUp { get; set; }

    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("written_on")]
    public DateTime WrittenOn { get; set; }
}

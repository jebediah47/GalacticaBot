using System.Text.Json.Serialization;

namespace GalacticaBot.Models;

public sealed class RedditMemeModel
{
    [JsonPropertyName("data")]
    public RedditData Data { get; set; } = new();
}

public sealed class RedditData
{
    [JsonPropertyName("children")]
    public List<RedditPost> Children { get; set; } = [];
}

public sealed class RedditPost
{
    [JsonPropertyName("data")]
    public RedditPostData Data { get; set; } = new();
}

public sealed class RedditPostData
{
    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("permalink")]
    public string Permalink { get; set; } = string.Empty;

    [JsonPropertyName("ups")]
    public int Ups { get; set; }

    [JsonPropertyName("num_comments")]
    public int NumComments { get; set; }
}

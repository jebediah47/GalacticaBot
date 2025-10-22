using System.Net.Http.Json;
using GalacticaBot.Models;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Fun;

public sealed class Meme(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    private static readonly Random Rnd = new();

    [SlashCommand("meme", "Grabs a random meme from the depths of Reddit")]
    public async Task Run()
    {
        string[] subreddits =
        [
            "r/meme",
            "r/memes",
            "r/terriblefacebookmemes",
            "r/dankmemes",
            "r/PewdiepieSubmissions",
            "r/MemeEconomy",
        ];

        var selectedSubreddit = subreddits[Rnd.Next(subreddits.Length)];

        var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("User-Agent", "request");
        var response = await http.GetAsync($"https://reddit.com/{selectedSubreddit}/.json");

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Generate("Failed to fetch meme. Please try again later.")]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<RedditMemeModel>();
        if (resp is null || resp.Data.Children.Count == 0)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Generate("No memes found. Please try again.")]
                    )
                )
            );

            return;
        }

        var randomPost = resp.Data.Children[Rnd.Next(resp.Data.Children.Count)].Data;

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Generate())
            .WithTitle(randomPost.Title)
            .WithUrl($"https://reddit.com{randomPost.Permalink}")
            .WithImage(new EmbedImageProperties(randomPost.Url))
            .WithFooter(
                new EmbedFooterProperties
                {
                    Text = $"👍 {randomPost.Ups} 💬 {randomPost.NumComments}",
                }
            );

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }
}

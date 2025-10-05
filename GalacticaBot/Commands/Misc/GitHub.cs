using System.Net.Http.Json;
using GalacticaBot.Models;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Misc;

public sealed class GitHub(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("github", "Allows you to view a user's GitHub profile")]
    public async Task Run(
        [SlashCommandParameter(Name = "username", Description = "The user to view")] string username
    )
    {
        var http = httpClientFactory.CreateClient();
        http.DefaultRequestHeaders.Add("User-Agent", "request");
        var response = await http.GetAsync($"https://api.github.com/users/{username}");

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [
                            GlobalErrorEmbed.Get(
                                "Failed to fetch user profile. Please try again later."
                            ),
                        ]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<GitHubUserModel>();

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Get())
            .WithTitle($"{resp!.Login}")
            .WithDescription(resp.Bio ?? "No bio")
            .WithFields(
                [
                    new EmbedFieldProperties
                    {
                        Name = "💖 Followers",
                        Value = $"```{resp.Followers.ToString()}```",
                        Inline = true,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "🏃 Following",
                        Value = $"```{resp.Following.ToString()}```",
                        Inline = true,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "📚 Repositories",
                        Value = $"```{resp.PublicRepos.ToString()}```",
                        Inline = true,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "✉️ Email",
                        Value = $"```{resp.Email ?? "No email"}```",
                        Inline = true,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "🏢 Company",
                        Value = $"```{resp.Company ?? "No company"}```",
                        Inline = true,
                    },
                    new EmbedFieldProperties
                    {
                        Name = "📍 Location",
                        Value = $"```{resp.Location ?? "No location"}```",
                        Inline = true,
                    },
                ]
            )
            .WithUrl(resp.HtmlUrl)
            .WithThumbnail(resp.AvatarUrl)
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }
}

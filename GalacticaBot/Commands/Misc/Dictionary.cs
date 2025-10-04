using System.Net.Http.Json;
using GalacticaBot.Models;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Misc;

public sealed class Dictionary(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("dictionary", "Searches Urban Dictionary for term definitions")]
    public async Task Run(
        [SlashCommandParameter(Name = "term", Description = "The term to search for")] string term
    )
    {
        var http = httpClientFactory.CreateClient();
        var encodedTerm = Uri.EscapeDataString(term);
        var response = await http.GetAsync(
            $"https://api.urbandictionary.com/v0/define?term={encodedTerm}"
        );

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [
                            GlobalErrorEmbed.Get(
                                "Failed to fetch definition. Please try again later."
                            ),
                        ]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<UrbanDictionaryModel>();
        if (resp is null || resp.List.Count == 0)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get($"No definitions found for **{term}**.")]
                    )
                )
            );

            return;
        }

        var answer = resp.List[0];

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Get())
            .WithTitle(answer.Word)
            .WithUrl(answer.Permalink)
            .WithFields(
                [
                    new EmbedFieldProperties
                    {
                        Name = "Definition:",
                        Value = Trim(answer.Definition),
                    },
                    new EmbedFieldProperties { Name = "Example:", Value = Trim(answer.Example) },
                    new EmbedFieldProperties
                    {
                        Name = "Ratings",
                        Value = $"{answer.ThumbsUp} 👍   {answer.ThumbsDown} 👎",
                    },
                ]
            )
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }

    private static string Trim(string input)
    {
        return input.Length > 1024 ? $"{input[..1020]} ... " : input;
    }
}

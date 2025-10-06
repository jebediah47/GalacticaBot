using System.Globalization;
using System.Net.Http.Json;
using GalacticaBot.Models;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Fun;

public sealed class Bored(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("bored", "Suggests a random activity to do when you're bored")]
    public async Task Run()
    {
        var http = httpClientFactory.CreateClient();
        var response = await http.GetAsync("https://bored-api.appbrewery.com/random");

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get("Failed to fetch activity. Please try again later.")]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<BoredActivityModel>();
        if (resp is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get("No activity found. Please try again.")]
                    )
                )
            );

            return;
        }

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Get())
            .WithTitle("Here's something for you to do!")
            .WithDescription(resp.Activity)
            .WithFields(
                [
                    new EmbedFieldProperties { Name = "Type", Value = Capitalize(resp.Type) },
                    new EmbedFieldProperties
                    {
                        Name = "Participants",
                        Value = $"`{resp.Participants}`",
                    },
                ]
            )
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }

    private static string Capitalize(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(input);
    }
}

using System.Net.Http.Json;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Misc;

public sealed class Kanye(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("kanye", "Sends a random quote from Kanye West")]
    public async Task Run()
    {
        var http = httpClientFactory.CreateClient();
        var response = await http.GetAsync("https://api.kanye.rest/");

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [
                            GlobalErrorEmbed.Get(
                                "Failed to fetch Kanye quote. Please try again later."
                            ),
                        ]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<KanyeQuote>();
        if (resp is null || string.IsNullOrWhiteSpace(resp.Quote))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get("Invalid response from Kanye quote service.")]
                    )
                )
            );

            return;
        }

        await RespondAsync(InteractionCallback.Message(resp.Quote));
    }

    private sealed class KanyeQuote
    {
        public string Quote { get; set; } = string.Empty;
    }
}

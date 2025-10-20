using System.Net.Http.Json;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Fun;

public class RandomDuck(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("randomduck", "Returns a image of a duck")]
    public async Task Run()
    {
        var http = httpClientFactory.CreateClient();
        var response = await http.GetAsync("https://random-d.uk/api/random");

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [
                            GlobalErrorEmbed.Get(
                                "Failed to get a random duck image. Please try again later."
                            ),
                        ]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<RandomDuckResponse>();
        if (resp is null || string.IsNullOrWhiteSpace(resp.Url))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get("Invalid response from random duck API service.")]
                    )
                )
            );

            return;
        }

        await RespondAsync(InteractionCallback.Message(resp.Url));
    }

    private record RandomDuckResponse(string Url, string Message);
}

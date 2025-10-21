using System.Net.Http.Json;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Fun;

public sealed class RandomDog(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("randomdog", "Returns a random image or video of a dog")]
    public async Task Run()
    {
        var http = httpClientFactory.CreateClient();
        var response = await http.GetAsync("https://random.dog/woof.json");

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [
                            GlobalErrorEmbed.Generate(
                                "Failed to get a random dog image. Please try again later."
                            ),
                        ]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<RandomDogResponse>();
        if (resp is null || string.IsNullOrWhiteSpace(resp.Url))
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Generate("Invalid response from random dog API service.")]
                    )
                )
            );

            return;
        }

        await RespondAsync(InteractionCallback.Message(resp.Url));
    }

    private sealed record RandomDogResponse(int FileSizeBytes, string Url);
}

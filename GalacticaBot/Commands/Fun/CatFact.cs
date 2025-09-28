using System.Net.Http.Json;
using GalacticaBot.Models;
using GalacticaBot.Utils;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Fun;

public sealed class CatFact(IHttpClientFactory httpClientFactory)
    : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("catfact", "Returns a random cat fact")]
    public async Task Run()
    {
        var http = httpClientFactory.CreateClient();
        var response = await http.GetAsync("https://catfact.ninja/fact");

        if (!response.IsSuccessStatusCode)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get("Failed to fetch cat fact. Please try again later.")]
                    )
                )
            );

            return;
        }

        var resp = await response.Content.ReadFromJsonAsync<CatFactModel>();
        if (resp is null)
        {
            await RespondAsync(
                InteractionCallback.Message(
                    new InteractionMessageProperties().WithEmbeds(
                        [GlobalErrorEmbed.Get("Invalid response from cat fact service.")]
                    )
                )
            );

            return;
        }

        await RespondAsync(InteractionCallback.Message($"**🐈 Here's a cat fact:** {resp.Fact}"));
    }
}

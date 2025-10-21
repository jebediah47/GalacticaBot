using GalacticaBot.Utils;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace GalacticaBot.Commands.Admin;

public sealed class Clear : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand(
        "clear",
        "Bulk deletes a specified amount of messages in the current channel",
        DefaultGuildPermissions = Permissions.ManageMessages,
        Contexts = [InteractionContextType.Guild]
    )]
    public async Task Run(
        [SlashCommandParameter(
            Name = "amount",
            Description = "The amount of messages to clear",
            MinValue = 1,
            MaxValue = 100
        )]
            int amount
    )
    {
        var channel = Context.Channel as TextGuildChannel;

        if (channel is null)
            InteractionCallback.Message(
                new InteractionMessageProperties().WithEmbeds(
                    [GlobalErrorEmbed.Generate("Invalid channel")]
                )
            );

        var maximumMessageAge = DateTimeOffset.UtcNow - TimeSpan.FromDays(14);

        var messagesQuery = await channel!
            .GetMessagesAsync(new PaginationProperties<ulong> { BatchSize = amount })
            .Take(amount)
            .ToListAsync();

        var messages = new List<ulong>();
        var tooOldCount = 0;

        foreach (var msg in messagesQuery)
        {
            if (msg.CreatedAt >= maximumMessageAge)
                messages.Add(msg.Id);
            else
                tooOldCount++;
        }

        await channel.DeleteMessagesAsync(messages);

        var description =
            tooOldCount > 0
                ? $"`{messages.Count}` messages were deleted, but `{tooOldCount}` messages weren't deleted due to being older than 14 days."
                : $"`{messages.Count}` messages were successfully deleted.";

        var embed = new EmbedProperties()
            .WithColor(RandomColor.Get())
            .WithTitle("🧹 Messages cleared!")
            .WithDescription(description)
            .WithTimestamp(DateTimeOffset.UtcNow);

        await RespondAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed]))
        );
    }
}

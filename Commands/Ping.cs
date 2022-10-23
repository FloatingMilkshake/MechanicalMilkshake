namespace MechanicalMilkshake.Commands;

public class Ping : ApplicationCommandModule
{
    [SlashCommand("ping", "Checks my ping.")]
    public static async Task PingCommand(InteractionContext ctx)
    {
        DiscordMessage message;
        try
        {
            message = await ctx.Channel.SendMessageAsync(
                "Pong! This is a temporary message used to check ping and should be deleted shortly.");
        }
        catch
        {
            // Round-trip ping failed because the bot doesn't have permission to send messages. That's fine though, we can still return client ping.

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                $"Pong! Client ping is `{ctx.Client.Ping}ms`.\n\nI tried to send a message to check round-trip ping, but I don't have permission to send messages in this channel! Try again in another channel where I have permission to send messages."));
            return;
        }

        var messageTimestampDateTime = DateTimeOffset
            .FromUnixTimeMilliseconds((long)IdHelpers.GetCreationTimestamp(message.Id, false)).UtcDateTime;

        var responseTime = (messageTimestampDateTime - ctx.Interaction.CreationTimestamp.UtcDateTime).ToString()
            .Replace("0", "")
            .Replace(":", "")
            .Replace(".", "");

        await message.DeleteAsync();

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
            $"Pong! Client ping is `{ctx.Client.Ping}ms`.\n\nIt took me `{responseTime}ms` to send a message after you used this command."));
    }
}
namespace MechanicalMilkshake.Commands;

public class Ping : ApplicationCommandModule
{
    [SlashCommand("ping", "Pong!")]
    public static async Task PingCommand(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Ping!"));
        var timeNow = DateTime.UtcNow;

        var websocketPing = ctx.Client.Ping;
        var msg = await ctx.Interaction.GetOriginalResponseAsync();
        var interactionLatency = Math.Round((timeNow - msg.CreationTimestamp.UtcDateTime).TotalMilliseconds);
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            $"Pong!\n"
            + $"Websocket ping: `{websocketPing}ms`\n"
            + $"Interaction latency: `{interactionLatency}ms`"));
    }
}
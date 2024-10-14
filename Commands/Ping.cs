namespace MechanicalMilkshake.Commands;

public class Ping
{
    [Command("ping")]
    [Description("Pong!")]
    public static async Task PingCommand(SlashCommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Ping!"));
        var timeNow = DateTime.UtcNow;

        var websocketPing = ctx.Client.GetConnectionLatency(
            ctx.Channel.IsPrivate
                ? ctx.Guild!.Id
                : Program.HomeServer.Id
            ).TotalMilliseconds;
        var msg = await ctx.Interaction.GetOriginalResponseAsync();
        var interactionLatency = Math.Round((timeNow - msg.CreationTimestamp.UtcDateTime).TotalMilliseconds);

        var dbPing = await DatabaseTasks.CheckDatabaseConnectionAsync();
        
        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(
            $"Pong!\n"
            + $"Websocket ping: `{websocketPing}ms`\n"
            + $"Interaction latency: `{interactionLatency}ms`\n"
            + $"Database ping: {(double.IsNaN(dbPing) ? "Unreachable!" : $"`{dbPing}ms`")}\n"
            + $"Uptime Kuma heartbeat status: `{Program.LastUptimeKumaHeartbeatStatus}`"));
    }
}
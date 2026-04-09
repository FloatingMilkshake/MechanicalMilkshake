namespace MechanicalMilkshake.Commands;

internal class PingCommands
{
    [Command("ping")]
    [Description("Pong!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    public static async Task PingCommandAsync(SlashCommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Ping!"));

        var websocketPing = ctx.Client.GetConnectionLatency(
            ctx.Channel.IsPrivate
                ? ctx.Guild?.Id ?? Setup.Configuration.Discord.HomeServer.Id
                : Setup.Configuration.Discord.HomeServer.Id
            ).TotalMilliseconds;
        var msg = await ctx.Interaction.GetOriginalResponseAsync();
        var interactionLatency = Math.Round((DateTime.UtcNow - msg.CreationTimestamp.UtcDateTime).TotalMilliseconds);

        var redisPing = await RedisTasks.CheckRedisConnectionAsync();

        var response = $"Pong!\n"
            + $"Websocket ping: `{websocketPing}ms`\n"
            + $"Interaction latency: `{interactionLatency}ms`\n";

        if (Setup.Configuration.ConfigJson.BotCommanders.Contains(ctx.User.Id.ToString()))
            response += $"Redis ping: {(double.IsNaN(redisPing) ? "Unreachable!" : $"`{redisPing}ms`")}\n"
            + $"Heartbeat: `{Setup.State.Process.LastUptimeKumaHeartbeatStatus}`";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
    }
}
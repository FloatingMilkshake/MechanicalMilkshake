namespace MechanicalMilkshake.Commands;

internal class PingCommands
{
    [Command("ping")]
    [Description("Pong!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task PingCommandAsync(SlashCommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent("Ping!")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));

        var websocketPing = ctx.Client.GetConnectionLatency(
            ctx.Channel.IsPrivate
                ? ctx.Guild?.Id ?? Setup.State.Discord.HomeServer.Id
                : Setup.State.Discord.HomeServer.Id
            ).TotalMilliseconds;
        var msg = await ctx.Interaction.GetOriginalResponseAsync();
        var interactionLatency = Math.Round((DateTime.UtcNow - msg.CreationTimestamp.UtcDateTime).TotalMilliseconds);

        var redisPing = await RedisTasks.CheckRedisConnectionAsync();

        var response = $"Pong!\n"
            + $"Websocket ping: `{websocketPing}ms`\n"
            + $"Interaction latency: `{interactionLatency}ms`\n";

        if (Setup.State.Process.Configuration.BotCommanders.Contains(ctx.User.Id.ToString()))
            response += $"Redis ping: {(double.IsNaN(redisPing) ? "Unreachable!" : $"`{redisPing}ms`")}\n"
            + $"Heartbeat: `{Setup.State.Process.LastUptimeKumaHeartbeatStatus}`";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
    }
}

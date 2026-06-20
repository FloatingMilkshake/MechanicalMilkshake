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

        var now = DateTime.UtcNow;

        var msg = await ctx.Interaction.GetOriginalResponseAsync();

        var websocketPing = ctx.Client.GetConnectionLatency(0).TotalMilliseconds;
        var interactionLatency = Math.Round((now - msg.CreationTimestamp.UtcDateTime).TotalMilliseconds);

        var response = $"Pong!\n"
            + $"Websocket ping: `{websocketPing}ms`\n"
            + $"Interaction latency: `{interactionLatency}ms`\n";

        if (Setup.State.Process.Configuration.BotCommanders.Contains(ctx.User.Id.ToString()))
            response += $"Heartbeat: `{Setup.State.Process.LastUptimeKumaHeartbeatStatus}`";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
    }
}

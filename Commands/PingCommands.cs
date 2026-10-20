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

        var msg = await ctx.GetResponseAsync();

        var websocketPing = ctx.Client.GetConnectionLatency(0).TotalMilliseconds;
        ulong interactionLatency = (msg.Id - ctx.Interaction.Id) >> 22;

        var response = $"Pong!\n"
            + $"Websocket ping: `{websocketPing}ms`\n"
            + $"Interaction latency: `{interactionLatency}ms`\n";

        if (Setup.State.Process.Configuration.BotCommanders.Contains(ctx.User.Id.ToString()))
            response += $"Heartbeat: `{Setup.State.Process.LastUptimeKumaHeartbeatStatus}`";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response));
    }
}

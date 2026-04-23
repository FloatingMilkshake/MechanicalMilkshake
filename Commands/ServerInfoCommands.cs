namespace MechanicalMilkshake.Commands;

internal class ServerInfoCommands
{
    [Command("serverinfo")]
    [Description("Look up information about this server.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild)]
    public static async Task ServerInfoCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder(await ctx.Guild.CreateInfoMessageAsync())
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }
}

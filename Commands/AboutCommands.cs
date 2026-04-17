namespace MechanicalMilkshake.Commands;

internal class AboutCommands
{
    [Command("about")]
    [Description("View information about me!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task AboutCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(false));

        const string privacyPolicyUrl = "https://floatingmilkshake.com/privacy#MechanicalMilkshake";

        const string supportServerInvite = "https://milkshake.wtf/bot/support";

        DiscordEmbedBuilder embed = new()
        {
            Title = $"About {ctx.Client.CurrentUser.Username}",
            Description =
                "Hi! I'm a multipurpose bot that can do a bunch of different stuff. If you want to see a list of"
                + " commands, hit `/` and pick me!",
            Color = Setup.Constants.BotColor
        };

        embed.AddField("Servers", ctx.Client.Guilds.Count.ToString(), true);
        embed.AddField("Commands", Setup.State.Discord.ApplicationCommands.Count.ToString(), true);

        if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
            embed.Description += $"\n\nMy Privacy Policy can be found [here]({privacyPolicyUrl})!";

        var remoteUrl = await File.ReadAllTextOrFallbackAsync("RemoteUrl.txt");
        if (remoteUrl != "") embed.AddField("Source Code Repository", remoteUrl);

        var botOwners = ctx.Client.CurrentApplication.Owners.ToList();

        var ownerOutput = botOwners.Count == 1
            ? $"The bot owner is @{botOwners.First().GetFullUsername()}."
            : "Bot owners are:" + string.Join("\n", botOwners.Select(o => $"- @{o.GetFullUsername()}"));

        embed.AddField("Owners", ownerOutput);

        embed.AddField("Need help?",
            "If you need help with the bot or would like to report an issue, there are a few ways to do so! You can:"
        + (string.IsNullOrWhiteSpace(supportServerInvite) ? "" : $"\n- Join the bot's [support server]({supportServerInvite})")
        + "\n- DM the bot itself (DMs are forwarded to owners!)"
        + "\n- DM a bot owner (see above for a list!)");

        await ctx.FollowupAsync(embed, ephemeral: ctx.ShouldUseEphemeralResponse(false));
    }

    [Command("version")]
    [Description("Show my version information.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task VersionCommandAsync(SlashCommandContext ctx,
        [Parameter("extended"), Description("Whether to show extended info. Defaults to False.")] bool extended = false)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(false));

        if (extended)
        {
            await ctx.FollowupAsync((await Setup.Types.DebugInfo.CreateDebugInfoEmbedAsync(false)).WithTitle("Version"),
                ephemeral: ctx.ShouldUseEphemeralResponse(false));
            return;
        }

        await ctx.FollowupAsync(new DiscordEmbedBuilder()
        {
            Title = "Version",
            Color = Setup.Constants.BotColor,
            Description = (await Setup.Types.DebugInfo.GetDebugInfoAsync()).CommitInformation
        }, ephemeral: ctx.ShouldUseEphemeralResponse(false));
    }
}

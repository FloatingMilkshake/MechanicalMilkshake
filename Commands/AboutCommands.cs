namespace MechanicalMilkshake.Commands;

internal class AboutCommands
{
    [Command("about")]
    [Description("View information about me!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    public static async Task AboutCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

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
        embed.AddField("Commands", Setup.State.Commands.ApplicationCommands.Count.ToString(), true);

        if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
            embed.Description += $"\n\nMy Privacy Policy can be found [here]({privacyPolicyUrl})!";

        var remoteUrl = await FileHelpers.ReadFileAsync("RemoteUrl.txt");
        if (remoteUrl != "") embed.AddField("Source Code Repository", remoteUrl);

        var botOwners = ctx.Client.CurrentApplication.Owners.ToList();

        var ownerOutput = botOwners.Count == 1
            ? $"The bot owner is @{UserInfoHelpers.GetFullUsername(botOwners.First())}."
            : "Bot owners are:" + string.Join("\n", botOwners.Select(o => $"- @{UserInfoHelpers.GetFullUsername(o)}"));

        embed.AddField("Owners", ownerOutput);

        embed.AddField("Need help?",
            "If you need help with the bot or would like to report an issue, there are a few ways to do so! You can:"
        + (string.IsNullOrWhiteSpace(supportServerInvite) ? "" : $"\n- Join the bot's [support server]({supportServerInvite})")
        + "\n- DM the bot itself (DMs are forwarded to owners!)"
        + "\n- DM a bot owner (see above for a list!)");

        await ctx.FollowupAsync(embed);
    }

    [Command("version")]
    [Description("Show my version information.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    public static async Task VersionCommandAsync(SlashCommandContext ctx,
        [Parameter("extended"), Description("Whether to show extended info. Defaults to False.")] bool extended = false)
    {
        await ctx.DeferResponseAsync();

        if (extended)
        {
            await ctx.FollowupAsync((await DebugInfoHelpers.GenerateDebugInfoEmbedAsync(false)).WithTitle("Version"));
            return;
        }

        await ctx.FollowupAsync(new DiscordEmbedBuilder()
        {
            Title = "Version",
            Color = Setup.Constants.BotColor,
            Description = (await Setup.Types.DebugInfo.GetDebugInfoAsync()).CommitInformation
        });
    }

    [Command("uptime")]
    [Description("Check my uptime!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    public static async Task UptimeCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        DiscordEmbedBuilder embed = new()
        {
            Title = "Uptime",
            Color = Setup.Constants.BotColor
        };

        var connectUnixTime = ((DateTimeOffset)Setup.State.Discord.ConnectTime).ToUnixTimeSeconds();

        var startTime = Setup.State.Process.ProcessStartTime;
        var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

        embed.AddField("Process started at", $"<t:{startUnixTime}:F> (<t:{startUnixTime}:R>)");
        embed.AddField("Last connected to Discord at", $"<t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>)");

        await ctx.FollowupAsync(embed);
    }
}

namespace MechanicalMilkshake.Commands;

public class AboutCommands : ApplicationCommandModule
{
    [SlashCommand("about", "View information about me!")]
    public static async Task About(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        // Set this to an empty string to disable the Privacy Policy notice in /about, or change it to your own
        // Privacy Policy URL if you have one.
        const string privacyPolicyUrl = "https://floatingmilkshake.com/privacy#MechanicalMilkshake";

        // Set this to an empty string to disable the Support Server notice in /about, or change it your own
        // Support Server invite if you have one.
        const string supportServerInvite = "https://floatingmilkshake.link/bot/support";

        // Create embed
        // Description
        DiscordEmbedBuilder embed = new()
        {
            Title = $"About {ctx.Client.CurrentUser.Username}",
            Description =
                "Hi! I'm a multipurpose bot that can do a bunch of different stuff. If you want to see a list of"
                + $" commands, hit `/` and pick me!",
            Color = Program.BotColor
        };

        // Server count, command count
        embed.AddField("Servers", ctx.Client.Guilds.Count.ToString(), true);
        embed.AddField("Commands", Program.ApplicationCommands.Count.ToString(), true);

        // Privacy Policy link; hidden if privacyPolicyUrl is empty.
        if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
            embed.Description += $"\n\nMy Privacy Policy can be found [here]({privacyPolicyUrl})!";

        // Repo link
        var remoteUrl = await FileHelpers.ReadFileAsync("RemoteUrl.txt", "");
        if (remoteUrl != "") embed.AddField("Source Code Repository", remoteUrl);

        // Bot owner info
        var botOwners = ctx.Client.CurrentApplication.Owners.ToList();

        var ownerOutput = botOwners.Count == 1
            ? $"The bot owner is @{UserInfoHelpers.GetFullUsername(botOwners.First())}."
            : botOwners.Aggregate("Bot owners are:",
                (current, owner) => current + $"\n- @{UserInfoHelpers.GetFullUsername(owner)}");

        embed.AddField("Owners", ownerOutput);

        // Need help?
        embed.AddField("Need help?",
            "If you need help with the bot or would like to report an issue, there are a few ways to do so! You can:"
        + (string.IsNullOrWhiteSpace(supportServerInvite) ? "" : $"\n- Join the bot's [support server]({supportServerInvite})")
        + $"\n- DM the bot itself (DMs are forwarded to owners!)"
        + $"\n- DM a bot owner (see above for a list!)");

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }

    [SlashCommand("version", "Show my version information.")]
    public static async Task CommitInfo(InteractionContext ctx,
        [Option("extended", "Whether to show extended info. Defaults to False.")] Boolean extended = false)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (extended) {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(
                await DebugInfoHelpers.GenerateDebugInfoEmbed(false)));
            return;
        }

        var commitHash = await FileHelpers.ReadFileAsync("CommitHash.txt", "dev");
        var commitMessage = await FileHelpers.ReadFileAsync("CommitMessage.txt", "dev");
        var commitUrl = await FileHelpers.ReadFileAsync("RemoteUrl.txt");

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(new DiscordEmbedBuilder()
            .WithColor(Program.BotColor)
            .AddField("Version", (commitHash == "dev" ? "`dev`" : $"[`{commitHash}`]({commitUrl}): {commitMessage}"))
            .AddField("Last updated on", DebugInfoHelpers.GetDebugInfo().CommitTimestamp)));
    }

    [SlashCommand("uptime", "Check my uptime!")]
        public static async Task Uptime(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = "Uptime",
                Color = Program.BotColor
            };

            var connectUnixTime = ((DateTimeOffset)Program.ConnectTime).ToUnixTimeSeconds();

            var startTime = Convert.ToDateTime(Program.ProcessStartTime);
            var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

            embed.AddField("Process started at", $"<t:{startUnixTime}:F> (<t:{startUnixTime}:R>)");
            embed.AddField("Last connected to Discord at", $"<t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>)");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }
}
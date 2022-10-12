namespace MechanicalMilkshake.Commands;

public class AboutCommands : ApplicationCommandModule
{
    [SlashCommand("about", "View information about the bot!")]
    public async Task About(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        // Set this to an empty string to disable the Privacy Policy notice in `/about`, or change it to your own Privacy Policy URL if you have one.
        var privacyPolicyUrl = "https://floatingmilkshake.com/privacy#MechanicalMilkshake";

        // Set this to an empty string to disable the Support Server notice in /about, or change it your own Support Server invite if you have one.
        var supportServerInvite = "https://link.floatingmilkshake.com/botsupport";

        DiscordEmbedBuilder embed = new()
        {
            Title = $"About {ctx.Client.CurrentUser.Username}",
            Description =
                $"A multipurpose bot with many miscellaneous commands. Type `/` and select {ctx.Client.CurrentUser.Username} to see available commands.",
            Color = Program.botColor
        };
        embed.AddField("Servers", ctx.Client.Guilds.Count.ToString(), true);
        embed.AddField("Total User Count (not unique)", ctx.Client.Guilds.Sum(g => g.Value.MemberCount).ToString(),
            true);

        int commandCount;
#if DEBUG
        commandCount = (await Program.discord.GetGuildApplicationCommandsAsync(Program.configjson.Base.HomeServerId))
            .Count;
#else
        commandCount = (await Program.discord.GetGlobalApplicationCommandsAsync()).Count;
#endif
        embed.AddField("Commands", commandCount.ToString(), true);

        if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
            embed.Description += $"\n\nThis bot's Privacy Policy can be found [here]({privacyPolicyUrl}).";

        if (!string.IsNullOrWhiteSpace(supportServerInvite))
            embed.Description += $"\nNeed help? Join the bot's support server [here]({supportServerInvite})!";

        // Commit hash / version
        var commitHash = "";
        if (File.Exists("CommitHash.txt"))
        {
            StreamReader readHash = new("CommitHash.txt");
            commitHash = readHash.ReadToEnd().Trim();
        }

        if (commitHash == "") commitHash = "dev";

        var remoteUrl = "";
        var commitUrl = "";
        if (File.Exists("RemoteUrl.txt"))
        {
            StreamReader readUrl = new("RemoteUrl.txt");
            remoteUrl = $"{readUrl.ReadToEnd().Trim()}";
            commitUrl = $"{remoteUrl}/commit/{commitHash}";
        }

        if (remoteUrl == "") remoteUrl = "N/A";

        if (commitUrl == "") commitUrl = "N/A";

        //embed.AddField("Version", $"[{commitHash}]({commitUrl})", true);
        embed.AddField("Source Code Repository", "https://github.com/FloatingMilkshake/MechanicalMilkshake");

        List<DiscordUser> botOwners = new();
        List<DiscordUser> authorizedUsers = new();

        foreach (var owner in ctx.Client.CurrentApplication.Owners) botOwners.Add(owner);

        foreach (var userId in Program.configjson.Base.AuthorizedUsers)
            authorizedUsers.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));

        string ownerOutput;
        if (botOwners.Count == 1)
        {
            ownerOutput = $"Bot owner is {botOwners.First().Username}#{botOwners.First().Discriminator}.";
        }
        else
        {
            ownerOutput = "Bot owners are:";
            foreach (var owner in botOwners) ownerOutput += $"\n- {owner.Username}#{owner.Discriminator}";
        }

        ownerOutput = ownerOutput.Trim() + "\n\nFor any issues with the bot, DM it or a bot owner.";

        ownerOutput = ownerOutput.Trim();

        embed.AddField("Owners", ownerOutput);

        var startTime = Convert.ToDateTime(Program.processStartTime);
        var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
        embed.AddField("Uptime", $"Up since <t:{startUnixTime}:F> (<t:{startUnixTime}:R>!)");

        embed.WithFooter(
            $"Using DSharpPlus {Program.discord.VersionString} and {RuntimeInformation.FrameworkDescription}\nRunning commit {commitHash}");

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }

    [SlashCommand("version", "Show version information.")]
    public async Task CommitInfo(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        var commitHash = "";
        if (File.Exists("CommitHash.txt"))
        {
            StreamReader readHash = new("CommitHash.txt");
            commitHash = readHash.ReadToEnd().Trim();
        }

        if (commitHash == "") commitHash = "dev";

        var commitMessage = "";
        if (File.Exists("CommitMessage.txt"))
        {
            StreamReader readMessage = new("CommitMessage.txt");
            commitMessage = readMessage.ReadToEnd().Trim();
        }

        if (commitMessage == "") commitMessage = "dev";

        string remoteUrl;
        var commitUrl = "";
        if (File.Exists("RemoteUrl.txt"))
        {
            StreamReader readUrl = new("RemoteUrl.txt");
            remoteUrl = $"{readUrl.ReadToEnd().Trim()}";
            commitUrl = $"{remoteUrl}/commit/{commitHash}";
        }

        await ctx.FollowUpAsync(
            new DiscordFollowupMessageBuilder().WithContent(
                $"Running commit [{commitHash}]({commitUrl}): \"{commitMessage}\""));
    }
}
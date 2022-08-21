namespace MechanicalMilkshake.Commands;

public class AboutCommands : ApplicationCommandModule
{
    [SlashCommand("about", "View information about the bot!")]
    public async Task About(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        // Set this to an empty string to disable the Privacy Policy notice in `/about`, or change it to your own Privacy Policy URL if you have one.
        string privacyPolicyUrl = "https://floatingmilkshake.com/privacy#MechanicalMilkshake";

        DiscordEmbedBuilder embed = new()
        {
            Title = $"About {ctx.Client.CurrentUser.Username}",
            Description =
                $"A multipurpose bot with many miscellaneous commands. Type `/` and select {ctx.Client.CurrentUser.Username} to see available commands.",
            Color = new DiscordColor("#FAA61A")
        };
        embed.AddField("Servers", ctx.Client.Guilds.Count.ToString(), true);
        embed.AddField("Total User Count (not unique)", ctx.Client.Guilds.Sum(g => g.Value.MemberCount).ToString(),
            true);

        if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
        {
            embed.Description += $"\n\nThis bot's Privacy Policy can be found [here]({privacyPolicyUrl}).";
        }

        // Unique user count
        List<DiscordUser> uniqueUsers = new();
        foreach (var guild in ctx.Client.Guilds)
        foreach (var member in guild.Value.Members)
        {
            var user = await ctx.Client.GetUserAsync(member.Value.Id);
            if (!uniqueUsers.Contains(user)) uniqueUsers.Add(user);
        }

        embed.AddField("Unique Users", uniqueUsers.Count.ToString(), true);

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

        embed.AddField("Version", $"[{commitHash}]({commitUrl})", true);
        embed.AddField("Source Code Repository", remoteUrl, true);

        List<DiscordUser> botOwners = new();
        List<DiscordUser> authorizedUsers = new();

        foreach (var owner in ctx.Client.CurrentApplication.Owners) botOwners.Add(owner);

        foreach (var userId in Program.configjson.AuthorizedUsers)
            authorizedUsers.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));

        var ownerOutput = "Bot owners are:";

        foreach (var owner in botOwners) ownerOutput += $"\n- {owner.Username}#{owner.Discriminator}";

        ownerOutput = ownerOutput.Trim() + "\n\nUsers authorized to use owner-level commands are:";

        foreach (var user in authorizedUsers) ownerOutput += $"\n- {user.Username}#{user.Discriminator}";

        ownerOutput = ownerOutput.Trim() + "\n\nFor any issues with the bot, DM it or one of these people:";
        foreach (var owner in botOwners) ownerOutput += $"\n- {owner.Username}#{owner.Discriminator}";

        ownerOutput = ownerOutput.Trim();

        embed.AddField("Owners", ownerOutput);

        var startTime = Convert.ToDateTime(Program.processStartTime);
        var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
        embed.AddField("Uptime", $"Up since <t:{startUnixTime}:F> (<t:{startUnixTime}:R>!)");

        embed.WithFooter(
            $"Using DSharpPlus {Program.discord.VersionString} and {RuntimeInformation.FrameworkDescription}");

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
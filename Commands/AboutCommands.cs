namespace MechanicalMilkshake.Commands;

public class AboutCommands : ApplicationCommandModule
{
    [SlashCommand("about", "View information about the bot!")]
    public static async Task About(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        // Set this to an empty string to disable the Privacy Policy notice in /about, or change it to your own Privacy Policy URL if you have one.
        const string privacyPolicyUrl = "https://floatingmilkshake.com/privacy#MechanicalMilkshake";

        // Set this to an empty string to disable the Support Server notice in /about, or change it your own Support Server invite if you have one.
        const string supportServerInvite = "https://floatingmilkshake.link/bot/support";

        DiscordEmbedBuilder embed = new()
        {
            Title = $"About {ctx.Client.CurrentUser.Username}",
            Description =
                $"A multipurpose bot with many miscellaneous commands. Type `/` and select {ctx.Client.CurrentUser.Username} to see available commands.",
            Color = Program.BotColor
        };
        embed.AddField("Servers", ctx.Client.Guilds.Count.ToString(), true);
        embed.AddField("Commands", Program.ApplicationCommands.Count.ToString(), true);

        // Privacy Policy link; hidden if privacyPolicyUrl is empty.
        if (!string.IsNullOrWhiteSpace(privacyPolicyUrl))
            embed.Description += $"\n\nThis bot's Privacy Policy can be found [here]({privacyPolicyUrl}).";

        // Support Server link; hidden if supportServerInvite is empty.
        if (!string.IsNullOrWhiteSpace(supportServerInvite))
            embed.Description += $"\nNeed help? Join the bot's support server [here]({supportServerInvite})!";

        // Commit hash / version
        var commitHash = await FileHelpers.ReadFileAsync("CommitHash.txt", "dev");
        var remoteUrl = await FileHelpers.ReadFileAsync("RemoteUrl.txt", "N/A");
        embed.AddField("Source Code Repository", remoteUrl);

        var botOwners = ctx.Client.CurrentApplication.Owners.ToList();

        var ownerOutput = botOwners.Count == 1
            ? $"Bot owner is {botOwners.First().Username}#{botOwners.First().Discriminator}."
            : botOwners.Aggregate("Bot owners are:",
                (current, owner) => current + $"\n- {owner.Username}#{owner.Discriminator}");

        ownerOutput = ownerOutput.Trim() +
                      "\n\nFor any issues with the bot, DM the bot itself or an owner. DMs sent to the bot are forwarded to owners.";

        embed.AddField("Owners", ownerOutput);

        var startTime = Convert.ToDateTime(Program.ProcessStartTime);
        var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
        embed.AddField("Uptime", $"Up since <t:{startUnixTime}:F> (<t:{startUnixTime}:R>!)");

        embed.WithFooter(
            $"Using DSharpPlus {Program.Discord.VersionString} and {RuntimeInformation.FrameworkDescription}\nRunning commit {commitHash}");

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }

    [SlashCommand("version", "Show version information.")]
    public static async Task CommitInfo(InteractionContext ctx)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        var commitHash = await FileHelpers.ReadFileAsync("CommitHash.txt", "dev");
        var commitMessage = await FileHelpers.ReadFileAsync("CommitMessage.txt", "dev");
        var commitUrl = await FileHelpers.ReadFileAsync("RemoteUrl.txt");

        await ctx.FollowUpAsync(
            new DiscordFollowupMessageBuilder().WithContent(
                $"Running commit [{commitHash}]({commitUrl}): \"{commitMessage}\""));
    }
}
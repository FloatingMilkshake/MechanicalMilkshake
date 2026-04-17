namespace MechanicalMilkshake.Commands;

[Command("debug")]
[Description("Commands for checking if the bot is working properly.")]
[RequireBotCommander]
[RequireHomeServer]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal class DebugCommands
{
    [Command("uptime")]
    [Description("Check my uptime!")]
    public static async Task UptimeCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        DiscordEmbedBuilder embed = new()
        {
            Title = "Uptime",
            Color = Setup.Constants.BotColor
        };

        var connectUnixTime = Setup.State.Discord.ConnectTime.ToUnixTimeSeconds();
        var startUnixTime = Setup.State.Process.ProcessStartTime.ToUnixTimeSeconds();

        embed.AddField("Process started at", $"<t:{startUnixTime}:F> (<t:{startUnixTime}:R>)");
        embed.AddField("Last connected to Discord at", $"<t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>)");

        await ctx.FollowupAsync(embed, ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));
    }

    [Command("timecheck")]
    [Description("Return the current time on the machine the bot is running on.")]
    public static async Task DebugTimeCheckCommandAsync(SlashCommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Time Check",
            Color = Setup.Constants.BotColor,
            Description = $"Seems to me like it's currently `{DateTime.Now:s}`."
        }).AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("shutdown")]
    [Description("Shut down the bot.")]
    public static async Task DebugShutdownCommandAsync(SlashCommandContext ctx)
    {
        DiscordButtonComponent shutdownButton = new(DiscordButtonStyle.Danger, "button-callback-debug-shutdown", "Shut Down");
        DiscordButtonComponent cancelButton = new(DiscordButtonStyle.Primary, "button-callback-debug-shutdown-cancel", "Cancel");

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent("Are you sure you want to shut down the bot? This action cannot be undone.")
            .AddActionRowComponent(shutdownButton, cancelButton)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("restart")]
    [Description("Restart the bot.")]
    public static async Task DebugRestartCommandAsync(SlashCommandContext ctx)
    {
        if (!File.Exists("/proc/self/cgroup"))
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"The bot may not be running under Docker; restart is unavailable. Use {"debug shutdown".AsSlashCommandMention()} if you wish to shut down the bot.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            return;
        }

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent("Restarting...")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        Environment.Exit(1);
    }

    [Command("owners")]
    [Description("Show the bot's owners.")]
    public static async Task DebugOwnersCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        DiscordEmbedBuilder embed = new()
        {
            Title = "Owners",
            Color = Setup.Constants.BotColor
        };

        List<DiscordUser> BotCommanders = [];

        var botOwners = ctx.Client.CurrentApplication.Owners.ToList();

        foreach (var userId in Setup.Configuration.ConfigJson.BotCommanders)
            BotCommanders.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));

        var botOwnerList = string.Join("\n", botOwners.Select(o => $"- @{o.GetFullUsername()} (`{o.Id}`)"));
        var botCommanderList = string.Join("\n", BotCommanders.Select(c => $"- @{c.GetFullUsername()} (`{c.Id}`)"));

        embed.AddField("Bot Owners", botOwnerList);
        embed.AddField("Bot Commanders",
            $"These users are authorized to use owner-level commands.\n{botCommanderList}");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("guilds")]
    [Description("Show the guilds that the bot is in.")]
    public static async Task DebugGuildsCommandAsync(SlashCommandContext ctx,
        [SlashChoiceProvider(typeof(Setup.Types.ChoiceProviders.GuildsListSortChoiceProvider))]
        [Parameter("sort_by"), Description("What to sort the list of guilds by.")] string sortBy = "",
        [SlashChoiceProvider(typeof(Setup.Types.ChoiceProviders.GuildsListSortDirectionChoiceProvider))]
        [Parameter("sort_direction"), Description("Which direction to sort the list of guilds.")] string sortDirection = "asc")
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        DiscordEmbedBuilder embed = new()
        {
            Title = $"Joined Guilds - {Setup.State.Discord.Client.Guilds.Count}",
            Color = Setup.Constants.BotColor
        };
        IReadOnlyList<DiscordGuild> sortedGuilds = sortBy switch
        {
            "name" => sortDirection == "asc"
                ? Setup.State.Discord.Client.Guilds.Values.OrderBy(g => g.Name).ToList()
                : Setup.State.Discord.Client.Guilds.Values.OrderByDescending(g => g.Name).ToList(),
            "joinDate" => sortDirection == "asc"
                ? Setup.State.Discord.Client.Guilds.Values.OrderBy(g => g.JoinedAt).ToList()
                : Setup.State.Discord.Client.Guilds.Values.OrderByDescending(g => g.JoinedAt).ToList(),
            _ => Setup.State.Discord.Client.Guilds.Values.ToList(),
        };
        foreach (var guild in Setup.State.Discord.Client.Guilds)
            embed.Description += $"- {guild.Value.Name}\n";

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("humandateparser")]
    [Description("See what happens when HumanDateParser tries to parse a date.")]
    public static async Task DebugHumanDateParserCommandAsync(SlashCommandContext ctx,
        [Parameter("date"), Description("The date (or time) for HumanDateParser to parse.")]
        string date)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        DiscordEmbedBuilder embed = new()
        {
            Title = "HumanDateParser Result",
            Color = Setup.Constants.BotColor
        };

        try
        {
            embed.WithDescription($"<t:{HumanDateParser.HumanDateParser.Parse(date).ToUnixTimeSeconds()}:F>");
        }
        catch (ParseException ex)
        {
            embed.WithDescription($"{ex.Message}");
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("checks")]
    [Description("Run the bot's timed checks manually.")]
    public static async Task DebugChecksCommandAsync(SlashCommandContext ctx,
        [Parameter("checks"), Description("The checks that should be run.")]
        [SlashChoiceProvider(typeof(Setup.Types.ChoiceProviders.ChecksChoiceProvider))]
        string checksToRun)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        // declare variables for check results
        int numRemindersBefore = default;
        int numRemindersAfter = default;
        int numRemindersSent = default;
        int numRemindersFailed = default;
        double redisPing = default;

        switch (checksToRun)
        {
            case "all":
                (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed) =
                    await ReminderTasks.CheckRemindersAsync();
                redisPing = await RedisTasks.CheckRedisConnectionAsync();
                break;
            case "reminders":
                (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed) =
                    await ReminderTasks.CheckRemindersAsync();
                break;
            case "redisConnection":
                redisPing = await RedisTasks.CheckRedisConnectionAsync();
                break;
        }

        // templates for check result messages

        // reminders
        var reminderCheckResultMessage = $"**Reminders:** "
                                    + $"Before: `{numRemindersBefore}`; "
                                    + $"After: `{numRemindersAfter}`; "
                                    + $"Sent: `{numRemindersSent}`; "
                                    + $"Failed: `{numRemindersFailed}`";

        // redis ping
        var redisPingResultMessage = $"**Redis ping:** {(double.IsNaN(redisPing) ? "Unreachable!" : $"`{redisPing}ms`")}";

        // set up response msg content
        // include relevant check results (see variables)
        var response = "Done!\n";
        switch (checksToRun)
        {
            case "all":
                response += $"{reminderCheckResultMessage}\n{redisPingResultMessage}";
                break;
            case "reminders":
                response += reminderCheckResultMessage;
                break;
            case "redisConnection":
                response += redisPingResultMessage;
                break;
        }

        // send response
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent(response)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("usage")]
    [Description("Show which commands are used the most.")]
    public static async Task DebugUsageCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        var cmdCounts = (from cmd in await Setup.Storage.Redis.HashGetAllAsync("commandCounts")
                         select new KeyValuePair<string, int>(cmd.Name, int.Parse(cmd.Value))).ToList();
        cmdCounts.Sort((x, y) => y.Value.CompareTo(x.Value));

        var output = string.Join("\n", cmdCounts.Select(c => $"{c.Key.AsSlashCommandMention()}: {c.Value}"));

        if (string.IsNullOrWhiteSpace(output))
            output = "I don't have any command counts saved!";

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent(output.Trim())
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("throw")]
    [Description("Intentionally throw an exception for debugging.")]
    public static async Task DebugThrowCommandAsync(SlashCommandContext ctx,
        [Parameter("exception"), Description("The type of exception to throw.")]
        [SlashChoiceProvider(typeof(Setup.Types.ChoiceProviders.TestExceptionChoiceProvider))]
        string exceptionType)
    {
        var exceptionFullName = exceptionType switch
        {
            "nullref" => "NullReferenceException",
            "invalidop" => "InvalidOperationException",
            "checksfailed" => "ChecksFailedException",
            _ => throw new NotSupportedException()
        };
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"Throwing {exceptionFullName}...")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));

        switch (exceptionType)
        {
            case "nullref":
                throw new NullReferenceException("This is a test exception");
            case "invalidop":
                throw new InvalidOperationException("This is a test exception");
            case "checksfailed":
                IReadOnlyList<ContextCheckFailedData> fakeErrorData = [];
                Command fakeCommand = default;
                throw new ChecksFailedException(fakeErrorData, fakeCommand, "This is a test exception");
        }
    }
}

namespace MechanicalMilkshake.Commands;

[Command("debug")]
[Description("Commands for checking if the bot is working properly.")]
[RequireBotCommander]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal static class DebugCommands
{
    [Command("blacklist")]
    [RequireApplicationOwner]
    public static class DebugBlacklistCommands
    {
        [Command("add")]
        [Description("Add a guild to the blacklist.")]
        public static async Task DebugBlacklistAddCommandAsync(SlashCommandContext ctx,
            [Parameter("guild"), Description("The ID of the guild to add to the blacklist.")] string guild,
            [Parameter("reason"), Description("The reason for blacklisting.")] string reason)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            ulong guildId;
            try
            {
                guildId = Convert.ToUInt64(guild);
            }
            catch (FormatException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I couldn't parse that guild ID!")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }

            var blacklistedGuilds = await Setup.Storage.Redis.HashGetAllAsync("blacklistedGuilds");

            if (blacklistedGuilds.Any(g => g.Name == guildId && g.Value == reason))
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("That guild is already blacklisted!")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }

            await Setup.Storage.Redis.HashSetAsync("blacklistedGuilds", guildId, reason);

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Blacklisted guild {guildId}.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("remove")]
        [Description("Remove a guild from the blacklist.")]
        public static async Task DebugBlacklistRemoveCommandAsync(SlashCommandContext ctx,
            [Parameter("guild"), Description("The ID of the guild to remove from the blacklist.")] string guild)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            ulong guildId;
            try
            {
                guildId = Convert.ToUInt64(guild);
            }
            catch (FormatException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I couldn't parse that guild ID!")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }

            var blacklistedGuilds = await Setup.Storage.Redis.HashGetAllAsync("blacklistedGuilds");

            if (blacklistedGuilds.All(g => g.Name != guildId))
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("That guild is not blacklisted!")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }

            await Setup.Storage.Redis.HashDeleteAsync("blacklistedGuilds", guildId);

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Removed guild {guildId} from the blacklist.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("list")]
        [Description("List blacklisted guilds.")]
        public static async Task DebugBlacklistListCommandAsync(SlashCommandContext ctx)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            var blacklistedGuilds = await Setup.Storage.Redis.HashGetAllAsync("blacklistedGuilds");

            string list;
            var blacklist = string.Join("\n", blacklistedGuilds.Select(g => $"{g.Name}: {g.Value}"));
            if (!string.IsNullOrWhiteSpace(blacklist))
                list = blacklist;
            else
                list = "No guilds are currently blacklisted.";

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(list)
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
    }

    [Command("guilds")]
    [Description("Commands for managing which guilds the bot is in.")]
    public static class DebugGuildsCommands
    {

        [Command("leave")]
        [Description("Makes the bot leave a guild. This cannot be undone!")]
        [RequireApplicationOwner]
        public static async Task DebugGuildsLeaveCommandAsync(SlashCommandContext ctx,
            [Parameter("guild"), Description("The ID of the guild to leave.")] string guildId)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            DiscordGuild guild = default;
            try
            {
                guild = ctx.Client.Guilds[Convert.ToUInt64(guildId)];
                await guild.LeaveAsync();
            }
            catch (FormatException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I couldn't parse that guild ID!")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            }

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Successfully left guild **{guild.Name}** ({guild.Id}).")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("list")]
        [Description("Show the guilds that the bot is in.")]
        public static async Task DebugGuildsListCommandAsync(SlashCommandContext ctx,
        [Parameter("filter"), Description("A space-separated list of guild IDs to filter to.")] string filter = "",
        [SlashChoiceProvider(typeof(DebugGuildsSortTypeChoiceProvider))]
        [Parameter("sort_by"), Description("What to sort the list of guilds by.")] string sortBy = "",
        [SlashChoiceProvider(typeof(DebugGuildsSortDirectionChoiceProvider))]
        [Parameter("sort_direction"), Description("Which direction to sort the list of guilds.")] string sortDirection = "asc")
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            List<DiscordGuild> guilds = [];
            if (filter == "")
            {
                guilds = ctx.Client.Guilds.Values.ToList();
            }
            else
            {
                var guildIds = filter.Split(' ');
                foreach (var guild in ctx.Client.Guilds.Values)
                {
                    if (guildIds.Contains(guild.Id.ToString()))
                        guilds.Add(guild);
                }
            }


            DiscordEmbedBuilder embed = new()
            {
                Title = $"Joined Guilds - {guilds.Count}",
                Color = Setup.Constants.BotColor
            };
            IReadOnlyList<DiscordGuild> sortedGuilds = sortBy switch
            {
                "name" => sortDirection == "asc"
                    ? guilds.OrderBy(g => g.Name).ToList()
                    : guilds.OrderByDescending(g => g.Name).ToList(),
                "joinDate" => sortDirection == "asc"
                    ? guilds.OrderBy(g => g.JoinedAt).ToList()
                    : guilds.OrderByDescending(g => g.JoinedAt).ToList(),
                _ => guilds.ToList(),
            };
            foreach (var guild in guilds)
                embed.Description += $"- {guild.Name}\n";

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .AddEmbed(embed)
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("count")]
        [Description("Get the number of guilds the bot is in.")]
        public static async Task DebugGuildsCountCommandAsync(SlashCommandContext ctx)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent(ctx.Client.Guilds.Count().ToString())
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
    }

    [Command("messagecache")]
    [Description("Get information about the in-memory message cache.")]
    public static class DebugCachedMessageCommands
    {
        [Command("channel")]
        [Description("Get the last cached message for a channel.")]
        public static async Task DebugCachedMessageChannelCommandAsync(SlashCommandContext ctx,
            [Parameter("channel"), Description("The channel to get the cached message for.")] ulong channelId)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            var success = Setup.State.Caches.MessageCache.TryGetMessageByChannel(channelId, out var message);
            if (success)
                await ctx.RespondAsync(await message.GetInformationAsync());
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("No message cached for that channel")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("message")]
        [Description("Get a cached message by its ID.")]
        public static async Task DebugCachedMessageMessageCommandAsync(SlashCommandContext ctx,
            [Parameter("message"), Description("The message to get the cached message for.")] ulong messageId)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            var success = Setup.State.Caches.MessageCache.TryGetMessage(messageId, out var message);
            if (success)
                await ctx.RespondAsync(await message.GetInformationAsync());
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("No message cached with that ID")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("author")]
        [Description("Get a cached message by its author's ID.")]
        public static async Task DebugCachedMessageAuthorCommandAsync(SlashCommandContext ctx,
            [Parameter("author"), Description("The author to get the cached message for.")] ulong authorId)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            var success = Setup.State.Caches.MessageCache.TryGetMessageByAuthor(authorId, out var message);
            if (success)
                await ctx.RespondAsync(await message.GetInformationAsync());
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("No message cached by that author")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("stats")]
        [Description("Get statistics about the in-memory message cache.")]
        public static async Task DebugCachedMessageStatsCommandAsync(SlashCommandContext ctx)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            var cachedMessagesCount = Setup.State.Caches.MessageCache.Count();
            var uniqueChannelsCount = Setup.State.Caches.MessageCache.GetUniqueChannelCount();
            var uniqueAuthorsCount = Setup.State.Caches.MessageCache.GetUniqueAuthorCount();

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"{cachedMessagesCount} messages in cache, from {uniqueAuthorsCount} author{(uniqueAuthorsCount == 1 ? "" : "s")} across {uniqueChannelsCount} channels.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

        [Command("newest")]
        [Description("Get the newest message from the message cache.")]
        public static async Task DebugCachedMessageNewestCommandAsync(SlashCommandContext ctx,
            [Parameter("skip"), Description("The number of messages to skip over.")] int skip = 0)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(await (Setup.State.Caches.MessageCache.GetNewestMessage(skip)).GetInformationAsync()));
        }

        [Command("oldest")]
        [Description("Get the oldest message from the message cache.")]
        public static async Task DebugCachedMessageOldestCommandAsync(SlashCommandContext ctx,
            [Parameter("skip"), Description("The number of messages to skip over.")] int skip = 0)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(await (Setup.State.Caches.MessageCache.GetOldestMessage(skip)).GetInformationAsync()));
        }
    }

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

        foreach (var userId in Setup.State.Process.Configuration.BotCommanders)
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
        [SlashChoiceProvider(typeof(DebugChecksChoiceProvider))]
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
        [SlashChoiceProvider(typeof(DebugThrowExceptionChoiceProvider))]
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

    private class DebugGuildsSortTypeChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("Name", "name"),
            new("Join Date", "joinDate")
        ];

        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }

    private class DebugGuildsSortDirectionChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("Ascending", "asc"),
            new("Descending", "desc")
        ];

        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }

    private class DebugChecksChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("All", "all"),
            new("Reminders", "reminders"),
            new("Redis Connection", "redisConnection")
        ];

        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }

    private class DebugThrowExceptionChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("NullReferenceException", "nullref"),
            new("InvalidOperationException", "invalidop"),
            new("ChecksFailedException", "checksfailed")
        ];

        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }
}

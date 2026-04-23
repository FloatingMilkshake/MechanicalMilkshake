namespace MechanicalMilkshake.Commands;

[Command("debug")]
[Description("Commands for checking if the bot is working properly.")]
[RequireBotCommander]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
[InteractionAllowedContexts([DiscordInteractionContextType.Guild])]
internal static class DebugCommands
{
    #region commands

    // The idea for this command, and a lot of the code, is taken from DSharpPlus/DSharpPlus.Test. Reference linked below.
    // https://github.com/DSharpPlus/DSharpPlus/blob/3a50fb3/DSharpPlus.Test/TestBotEvalCommands.cs
    [Command("eval")]
    [Description("Evaluate C# code!")]
    public static async Task DebugEvalCommandAsync(SlashCommandContext ctx, [Parameter("code"), Description("The code to evaluate.")] string code)
    {
        CancellationToken cancellationToken = default;

        var builder = new DiscordMessageBuilder().WithContent("Working on it...");

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder(builder)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        var msg = await ctx.GetResponseAsync();

        if (Setup.Eval.RestrictedTerms.Any(code.Contains))
        {
            await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("You can't do that."));
            return;
        }

        try
        {
            var scriptOptions = ScriptOptions.Default;
            scriptOptions = scriptOptions.WithImports(Setup.Eval.Imports);
            scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

            var script = CSharpScript.Create(code, scriptOptions, typeof(Setup.Eval.Globals));

            // Only offer the option to cancel if the code being evaluated supports it.
            if (code.Contains("CToken"))
            {
                builder.AddActionRowComponent(new DiscordActionRowComponent(
                    [new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-eval-cancel", "Cancel")]
                ));

                Setup.State.Caches.CancellationTokens.Add(msg.Id, new CancellationTokenSource());
                cancellationToken = Setup.State.Caches.CancellationTokens[msg.Id].Token;

                await ctx.EditResponseAsync(builder);
            }

            var result = await script.RunAsync(new Setup.Eval.Globals(Setup.State.Discord.Client, ctx, cancellationToken), cancellationToken).ConfigureAwait(false);

            if (result?.ReturnValue is null)
            {
                await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("null"));
            }
            else
            {
                if (string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                {
                    // Isn't null, so it has to be whitespace
                    await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent($"\"{result.ReturnValue}\""));
                    return;
                }

                var splitOutput = result.ReturnValue.ToString().SplitForDiscord();

                foreach (var part in splitOutput)
                {
                    await ctx.Channel.SendMessageAsync(part);
                }

                if (cancellationToken.IsCancellationRequested)
                    await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
                else
                    await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("Done!"));
            }
        }
        catch (Exception e)
        {
            if (cancellationToken.IsCancellationRequested)
                await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
            else
                await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent(e.GetType() + ": " + e.Message));
        }

        Setup.State.Caches.CancellationTokens.Remove(msg.Id);
    }

    // The idea for this command, and a lot of the code, is taken from Erisa's Lykos. References are linked below.
    // https://github.com/Erisa/Lykos/blob/5f9c17c/src/Modules/Owner.cs#L116-L144
    // https://github.com/Erisa/Lykos/blob/822e9c5/src/Modules/Helpers.cs#L36-L82
    [Command("shell")]
    [Description("Run a shell command on the machine the bot's running on!")]
    public static async Task DebugShellCommandAsync(SlashCommandContext ctx,
        [Parameter("command"), Description("The command to run, including any arguments.")]
        string command)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent("Working on it...")
            .AddActionRowComponent(new DiscordActionRowComponent(
                [new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-eval-cancel", "Cancel")]
            ))
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false))
        );

        if (Setup.Eval.RestrictedTerms.Any(command.Contains))
            if (!Setup.State.Discord.Client.CurrentApplication.Owners.Contains(ctx.User))
            {
                await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
                return;
            }

        // hardcode protection for SSH on my instance of the bot
        if (Setup.State.Discord.Client.CurrentUser.Id == 863140071980924958
            && ctx.User.Id != 455432936339144705
            && command.Contains("ssh"))
        {
            await ctx.EditResponseAsync(new DiscordFollowupMessageBuilder().WithContent("You can't do that."));
            return;
        }

        var msg = await ctx.GetResponseAsync();
        Setup.State.Caches.CancellationTokens.Add(msg.Id, new CancellationTokenSource());
        var cancellationToken = Setup.State.Caches.CancellationTokens[msg.Id].Token;

        var cmdResponse = await RunShellCommandAsync(command, cancellationToken);

        if (cancellationToken.IsCancellationRequested)
        {
            await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent("The operation was cancelled."));
            Setup.State.Caches.CancellationTokens.Remove(msg.Id);
            return;
        }

        var splitOutput = $"```\n{cmdResponse.Output}\n{cmdResponse.Error}\n```".SplitForDiscord();

        foreach (var part in splitOutput)
        {
            await ctx.Channel.SendMessageAsync(part);
        }
        await ctx.EditResponseAsync(new DiscordMessageBuilder().WithContent($"\nFinished with exit code `{cmdResponse.ExitCode}`."));

        Setup.State.Caches.CancellationTokens.Remove(msg.Id);
    }

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
        [Command("info")]
        [Description("Get information about a guild.")]
        public static async Task DebugGuildsInfoCommandAsync(SlashCommandContext ctx,
            [SlashAutoCompleteProvider(typeof(DebugGuildsGuildSearchAutoCompleteProvider))]
            [Parameter("guild"), Description("The guild to get info for. Accepts names or IDs.")] string guildId)
        {
            await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

            DiscordGuild guild;
            try
            {
                guild = ctx.Client.Guilds[Convert.ToUInt64(guildId)];
            }
            catch (FormatException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("I couldn't parse that guild. I need to be in the guild and you must provide a name (choose from AutoComplete) or ID.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }
            catch (KeyNotFoundException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Your guild ID was invalid, or I'm not in the guild. I need to be in the guild and you must provide a name (choose from AutoComplete) or ID.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder(await guild.CreateInfoMessageAsync())
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }

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
            var uniqueGuildsCount = Setup.State.Caches.MessageCache.GetUniqueGuildCount();
            var uniqueAuthorsCount = Setup.State.Caches.MessageCache.GetUniqueAuthorCount();

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"{cachedMessagesCount} messages in cache, from {uniqueAuthorsCount} author{(uniqueAuthorsCount == 1 ? "" : "s")}, {uniqueChannelsCount} channel{(uniqueChannelsCount == 1 ? "" : "s")}, {uniqueGuildsCount} guild{(uniqueGuildsCount == 1 ? "" : "s")}.")
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

    #endregion commands

    #region '/debug shell' utilities

    private static async Task<ShellCommandResult> RunShellCommandAsync(string command, CancellationToken cancellationToken)
    {
        var osDescription = RuntimeInformation.OSDescription;
        string fileName;
        string args;
        var escapedArgs = command.Replace("\"", "\\\"");

        if (osDescription.Contains("Windows"))
        {
            fileName = @"C:\Program Files\PowerShell\7\pwsh.exe";
            args = $"-Command \"$PSStyle.OutputRendering = [System.Management.Automation.OutputRendering]::PlainText ; {escapedArgs} 2>&1\"";
        }
        else
        {
            // Assume Linux if OS is not Windows because I'm too lazy to bother with specific checks right now, might implement that later
            fileName = Environment.GetEnvironmentVariable("SHELL");
            if (!File.Exists(fileName)) fileName = "/bin/sh";

            args = $"-c \"{escapedArgs}\"";
        }

        Process proc = new()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true
            }
        };

        proc.Start();
        string result;
        try
        {
            result = await proc.StandardOutput.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            result = "The operation was cancelled.";
        }
        if (cancellationToken.IsCancellationRequested)
            proc.Kill();

        // Wait a bit for the process to be killed
        await Task.Delay(5000, CancellationToken.None);

        return new ShellCommandResult(proc.ExitCode, HideSensitiveInfo(result));
    }

    private static string HideSensitiveInfo(string input)
    {
        const string redacted = "[redacted]";
        var output = input.Replace(Setup.State.Process.Configuration.BotToken, redacted);
        if (!string.IsNullOrWhiteSpace(Setup.State.Process.Configuration.WolframAlphaAppId))
            output = output.Replace(Setup.State.Process.Configuration.WolframAlphaAppId, redacted);
        if (!string.IsNullOrWhiteSpace(Setup.State.Process.Configuration.DbotsApiToken))
            output = output.Replace(Setup.State.Process.Configuration.DbotsApiToken, redacted);

        return output;
    }

    private class ShellCommandResult
    {
        internal ShellCommandResult(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }

        internal ShellCommandResult(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
            Error = default;
        }

        internal int ExitCode { get; private set; }
        internal string Output { get; private set; }
        internal string Error { get; private set; }
    }

    #endregion '/debug shell' utilities

    #region choice/autocomplete providers

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

    private class DebugGuildsGuildSearchAutoCompleteProvider : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            var focusedOption = ctx.Options.FirstOrDefault(x => x.Focused);

            if (focusedOption is not null)
            {
                return ctx.Client.Guilds.Values.Where(g => g.Name.Contains(focusedOption.Value.ToString(), StringComparison.OrdinalIgnoreCase)
                        || g.Id.ToString().Contains(focusedOption.Value.ToString()))
                    .Select(guild => new DiscordAutoCompleteChoice(guild.Name, guild.Id.ToString())).Take(25).ToList();
            }
            return default;
        }
    }

    #endregion choice/autocomplete providers
}

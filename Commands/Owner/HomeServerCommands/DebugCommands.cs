namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

[SlashRequireAuth]
public class DebugCommands : ApplicationCommandModule
{
    [SlashCommandGroup("debug", "Commands for checking if the bot is working properly.")]
    public class DebugCmds : ApplicationCommandModule
    {
        [SlashCommand("timecheck",
            "Return the current time on the machine the bot is running on.")]
        public static async Task TimeCheck(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Time Check", Color = Program.BotColor,
                Description = $"Seems to me like it's currently `{DateTime.Now}`."
            }));
        }

        [SlashCommand("shutdown", "Shut down the bot.")]
        public static async Task Shutdown(InteractionContext ctx)
        {
            DiscordButtonComponent shutdownButton = new(ButtonStyle.Danger, "shutdown-button", "Shut Down");
            DiscordButtonComponent cancelButton = new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel");

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent("Are you sure you want to shut down the bot? This action cannot be undone.")
                .AddComponents(shutdownButton, cancelButton));
        }

        [SlashCommand("restart", "Restart the bot.")]
        public static async Task Restart(InteractionContext ctx)
        {
            try
            {
                var dockerCheckFile = await File.ReadAllTextAsync("/proc/self/cgroup");
                if (string.IsNullOrWhiteSpace(dockerCheckFile))
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                        $"The bot may not be running under Docker; this means that {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "restart")} will behave like {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")}."
                        + $"\n\nOperation aborted. Use {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")} if you wish to shut down the bot."));
                    return;
                }
            }
            catch
            {
                // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"The bot may not be running under Docker; this means that {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "restart")} will behave like {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")}."
                    + $"\n\nOperation aborted. Use {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")} if you wish to shut down the bot."));
                return;
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Restarting..."));
            Environment.Exit(1);
        }

        [SlashCommand("owners", "Show the bot's owners.")]
        public static async Task Owners(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = "Owners",
                Color = Program.BotColor
            };

            List<DiscordUser> authorizedUsers = [];

            var botOwners = ctx.Client.CurrentApplication.Owners.ToList();

            foreach (var userId in Program.ConfigJson.Base.AuthorizedUsers)
                authorizedUsers.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));

            var botOwnerList = botOwners.Aggregate("",
                (current, owner) => current + $"\n- {UserInfoHelpers.GetFullUsername(owner)} (`{owner.Id}`)");

            var authUsersList = authorizedUsers.Aggregate("",
                (current, user) => current + $"\n- {UserInfoHelpers.GetFullUsername(user)} (`{user.Id}`)");

            embed.AddField("Bot Owners", botOwnerList);
            embed.AddField("Authorized Users",
                $"These users are authorized to use owner-level commands.\n{authUsersList}");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("guilds", "Show the guilds that the bot is in.")]
        public static async Task Guilds(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = $"Joined Guilds - {Program.Discord.Guilds.Count}",
                Color = Program.BotColor
            };

            foreach (var guild in Program.Discord.Guilds)
                embed.Description += $"- `{guild.Value.Id}`: {guild.Value.Name}\n";

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("humandateparser",
            "See what happens when HumanDateParser tries to parse a date.")]
        public static async Task HumanDateParserCmd(InteractionContext ctx,
            [Option("date", "The date (or time) for HumanDateParser to parse.")]
            string date)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = "HumanDateParser Result",
                Color = Program.BotColor
            };

            try
            {
                embed.WithDescription(
                    $"<t:{((DateTimeOffset)HumanDateParser.HumanDateParser.Parse(date)).ToUnixTimeSeconds()}:F>");
            }
            catch (ParseException ex)
            {
                embed.WithDescription($"{ex.Message}");
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("checks", "Run the bot's timed checks manually.")]
        public static async Task DebugChecks(InteractionContext ctx,
            [Option("checks", "The checks that should be run.")]
            [Choice("All", "all")]
            [Choice("Reminders", "reminders")]
            [Choice("Package Updates", "packageupdates")]
            string checksToRun)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            switch (checksToRun)
            {
                case "all":
                    await ReminderChecks.ReminderCheck();
                    await PackageUpdateChecks.PackageUpdateCheck();
                    break;
                case "reminders":
                    await ReminderChecks.ReminderCheck();
                    break;
                case "packageupdates":
                    await PackageUpdateChecks.PackageUpdateCheck();
                    break;
                case "databaseConnection":
                    await DatabaseChecks.CheckDatabaseConnectionAsync();
                    break;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Done!"));
        }

        [SlashCommand("usage", "Show which commands are used the most.")]
        public static async Task Usage(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var cmdCounts = (from cmd in await Program.Db.HashGetAllAsync("commandCounts")
                select new KeyValuePair<string, int>(cmd.Name, int.Parse(cmd.Value))).ToList();
            cmdCounts.Sort((x, y) => y.Value.CompareTo(x.Value));

            var output = cmdCounts.Aggregate("",
                (current, cmd) => current + $"{SlashCmdMentionHelpers.GetSlashCmdMention(cmd.Key)}: {cmd.Value}\n");

            if (string.IsNullOrWhiteSpace(output))
                output = "I don't have any command counts saved!";

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(output.Trim()));
        }

        [SlashCommand("throw", "Intentionally throw an exception for debugging.")]
        public static async Task Error(InteractionContext ctx,
            [Option("exception", "The type of exception to throw.")]
            [Choice("NullReferenceException", "nullref")]
            [Choice("InvalidOperationException", "invalidop")]
            [Choice("SlashExecutionChecksFailedException", "slashchecksfailed")]
            string exceptionType)
        {
            var exceptionFullName = exceptionType switch
            {
                "nullref" => "NullReferenceException",
                "invalidop" => "InvalidOperationException",
                "slashchecksfailed" => "SlashExecutionChecksFailedException",
                _ => throw new NotImplementedException()
            };
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                $"Throwing {exceptionFullName}..."));

            switch (exceptionType)
            {
                case "nullref":
                    throw new NullReferenceException("This is a test exception");
                case "invalidop":
                    throw new InvalidOperationException("This is a test exception");
                case "slashchecksfailed":
                    throw new SlashExecutionChecksFailedException();
            }
        }
    }
}
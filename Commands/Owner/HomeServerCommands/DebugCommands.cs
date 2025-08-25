namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

[Command("debug")]
[Description("Commands for checking if the bot is working properly.")]
[RequireAuth]
public class DebugCmds
{
    [Command("timecheck")]
    [Description("Return the current time on the machine the bot is running on.")]
    public static async Task TimeCheck(SlashCommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Time Check", Color = Program.BotColor,
            Description = $"Seems to me like it's currently `{DateTime.Now:s}`."
        }));
    }

    [Command("shutdown")]
    [Description("Shut down the bot.")]
    public static async Task Shutdown(SlashCommandContext ctx)
    {
        DiscordButtonComponent shutdownButton = new(DiscordButtonStyle.Danger, "shutdown-button", "Shut Down");
        DiscordButtonComponent cancelButton = new(DiscordButtonStyle.Primary, "shutdown-cancel-button", "Cancel");

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent("Are you sure you want to shut down the bot? This action cannot be undone.")
            .AddActionRowComponent(shutdownButton, cancelButton));
    }

    [Command("restart")]
    [Description("Restart the bot.")]
    public static async Task Restart(SlashCommandContext ctx)
    {
        try
        {
            var dockerCheckFile = await File.ReadAllTextAsync("/proc/self/cgroup");
            if (string.IsNullOrWhiteSpace(dockerCheckFile))
            {
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"The bot may not be running under Docker; this means that {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "restart")} will behave like {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")}."
                    + $"\n\nOperation aborted. Use {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")} if you wish to shut down the bot."));
                return;
            }
        }
        catch
        {
            // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(
                $"The bot may not be running under Docker; this means that {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "restart")} will behave like {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")}."
                + $"\n\nOperation aborted. Use {SlashCmdMentionHelpers.GetSlashCmdMention("debug", "shutdown")} if you wish to shut down the bot."));
            return;
        }

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Restarting..."));
        Environment.Exit(1);
    }

    [Command("owners")]
    [Description("Show the bot's owners.")]
    public static async Task Owners(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        DiscordEmbedBuilder embed = new()
        {
            Title = "Owners",
            Color = Program.BotColor
        };

        List<DiscordUser> BotCommanders = [];

        var botOwners = ctx.Client.CurrentApplication.Owners.ToList();

        foreach (var userId in Program.ConfigJson.BotCommanders)
            BotCommanders.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));

        var botOwnerList = botOwners.Aggregate("",
            (current, owner) => current + $"\n- {UserInfoHelpers.GetFullUsername(owner)} (`{owner.Id}`)");

        var authUsersList = BotCommanders.Aggregate("",
            (current, user) => current + $"\n- {UserInfoHelpers.GetFullUsername(user)} (`{user.Id}`)");

        embed.AddField("Bot Owners", botOwnerList);
        embed.AddField("Authorized Users",
            $"These users are authorized to use owner-level commands.{authUsersList}");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }

    [Command("guilds")]
    [Description("Show the guilds that the bot is in.")]
    public static async Task Guilds(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        DiscordEmbedBuilder embed = new()
        {
            Title = $"Joined Guilds - {Program.Discord.Guilds.Count}",
            Color = Program.BotColor
        };

        foreach (var guild in Program.Discord.Guilds)
            embed.Description += $"- `{guild.Value.Id}`: {guild.Value.Name}\n";

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }

    [Command("humandateparser")]
    [Description("See what happens when HumanDateParser tries to parse a date.")]
    public static async Task HumanDateParserCmd(SlashCommandContext ctx,
        [Parameter("date"), Description("The date (or time) for HumanDateParser to parse.")]
        string date)
    {
        await ctx.DeferResponseAsync();

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

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }

    [Command("checks")]
    [Description("Run the bot's timed checks manually.")]
    public static async Task DebugChecks(SlashCommandContext ctx,
        [Parameter("checks"), Description("The checks that should be run.")]
        [SlashChoiceProvider(typeof(ChecksChoiceProvider))]
        string checksToRun)
    {
        await ctx.DeferResponseAsync();

        // declare variables for check results
        int numRemindersBefore = default;
        int numRemindersAfter = default;
        int numRemindersSent = default;
        int numRemindersFailed = default;
        int numRemindersWithNullTime = default;
        double dbPing = default;

        switch (checksToRun)
        {
            case "all":
                (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed, numRemindersWithNullTime) =
                    await ReminderTasks.CheckRemindersAsync();
                dbPing = await DatabaseTasks.CheckDatabaseConnectionAsync();
                break;
            case "reminders":
                (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed, numRemindersWithNullTime) =
                    await ReminderTasks.CheckRemindersAsync();
                break;
            case "databaseConnection":
                dbPing = await DatabaseTasks.CheckDatabaseConnectionAsync();
                break;
        }
        
        // templates for check result messages
        
        // reminders
        var reminderCheckResultMessage = $"**Reminders:** "
                                    + $"Before: `{numRemindersBefore}`; "
                                    + $"After: `{numRemindersAfter}`; "
                                    + $"Sent: `{numRemindersSent}`; "
                                    + $"Failed: `{numRemindersFailed}`; "
                                    + $"Null Time: `{numRemindersWithNullTime}`";
        
        // database ping
        var dbPingResultMessage = $"**Database ping:** {(double.IsNaN(dbPing) ? "Unreachable!" : $"`{dbPing}ms`")}";
        
        // set up response msg content
        // include relevant check results (see variables)
        var response = "Done!\n";
        switch (checksToRun)
        {
            case "all":
                response += $"{reminderCheckResultMessage}\n{dbPingResultMessage}";
                break;
            case "reminders":
                response += reminderCheckResultMessage;
                break;
            case "databaseConnection":
                response += dbPingResultMessage;
                break;
        }
        
        // send response
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }

    [Command("usage")]
    [Description("Show which commands are used the most.")]
    public static async Task Usage(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        var cmdCounts = (from cmd in await Program.Db.HashGetAllAsync("commandCounts")
            select new KeyValuePair<string, int>(cmd.Name, int.Parse(cmd.Value))).ToList();
        cmdCounts.Sort((x, y) => y.Value.CompareTo(x.Value));

        var output = cmdCounts.Aggregate("",
            (current, cmd) => current + $"{SlashCmdMentionHelpers.GetSlashCmdMention(cmd.Key)}: {cmd.Value}\n");

        if (string.IsNullOrWhiteSpace(output))
            output = "I don't have any command counts saved!";

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(output.Trim()));
    }

    [Command("throw")]
    [Description("Intentionally throw an exception for debugging.")]
    public static async Task Error(SlashCommandContext ctx,
        [Parameter("exception"), Description("The type of exception to throw.")]
        [SlashChoiceProvider(typeof(TestExceptionChoiceProvider))]
        string exceptionType)
    {
        var exceptionFullName = exceptionType switch
        {
            "nullref" => "NullReferenceException",
            "invalidop" => "InvalidOperationException",
            "checksfailed" => "ChecksFailedException",
            _ => throw new NotSupportedException()
        };
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(
            $"Throwing {exceptionFullName}..."));

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
    
    private class ChecksChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("All", "all"),
            new("Reminders", "reminders"),
            new("Database Connection", "databaseConnection")
        ];
        
        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }
    
    private class TestExceptionChoiceProvider : IChoiceProvider
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
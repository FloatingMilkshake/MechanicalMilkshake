namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

[Command("activity")]
[Description("Configure the bot's activity!")]
public class ActivityCmds
{
    [Command("add")]
    [Description("Add a custom status message to the list that the bot cycles through, or modify an existing entry.")]
    public static async Task AddActivity(SlashCommandContext ctx,
        [Parameter("type"), Description("The type of status (playing, watching, etc).")]
        [SlashChoiceProvider(typeof(ActivityTypeChoiceProvider))]
        string type,
        [Parameter("message"), Description("The message to show after the status type.")] [MinMaxLength(maxLength: 128)]
        string message)
    {
        await ctx.DeferResponseAsync();

        await Program.Db.HashSetAsync("customStatusList", message, type);

        await ctx.FollowupAsync(
            new DiscordFollowupMessageBuilder().WithContent("Activity added successfully!"));
    }

    [Command("list")]
    [Description("List the custom status messages that the bot cycles through.")]
    public static async Task ListActivity(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        // Get list of custom statuses; if empty, show error & return
        var dbList = await Program.Db.HashGetAllAsync("customStatusList");
        if (dbList.Length == 0)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"There are no custom status messages in the list! Add some with {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "add")}."));
            return;
        }

        // Using list, create numbered list to display
        var output = "";
        var index = 1;
        foreach (var item in dbList)
        {
            output +=
                $"{index}: **{item.Value.ToString().First().ToString().ToUpper() + item.Value.ToString()[1..]}** {item.Name}\n";
            index++;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(output));
    }

    [Command("choose")]
    [Description("Choose a custom status message from the list to set now.")]
    public static async Task ChooseActvity(SlashCommandContext ctx,
        [Parameter("id"), Description("The ID number of the status to set. You can get this with /activity list.")]
        long id)
    {
        await ctx.DeferResponseAsync();

        // Get custom statuses, loop through them, and set the one at the matching index
        var dbList = await Program.Db.HashGetAllAsync("customStatusList");
        var index = 1;
        foreach (var item in dbList)
        {
            if (id != index)
            {
                index++;
                continue;
            }

            // Try to format stored activity as a DiscordActivity; on failure, show error & return
            var (success, activity) = ParseActivityType(new DiscordActivity { Name = item.Name }, item);
            if (!success)
            {
                await ctx.FollowupAsync(
                    new DiscordFollowupMessageBuilder().AddEmbed(GetCustomStatusErrorEmbed(item)));
                return;
            }

            // Replace keywords like {activity} or {serverCount} with the respective values
            activity = await ReplaceActivityKeywords(activity);

            await Program.Discord.UpdateStatusAsync(activity, DiscordUserStatus.Online);
            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent("Activity updated successfully!"));
            break;
        }
    }

    [Command("remove")]
    [Description("Remove a custom status message from the list that the bot cycles through.")]
    public static async Task RemoveActivity(SlashCommandContext ctx,
        [Parameter("id"), Description("The ID number of the status to remove. You can get this with /activity list.")]
        long id)
    {
        await ctx.DeferResponseAsync();

        var dbList = await Program.Db.HashGetAllAsync("customStatusList");

        if (dbList.Length == 0)
        {
            // List is empty; nothing to remove
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"There are no custom status messages in the list! Add some with {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "add")}."));
            return;
        }

        var index = 1;
        foreach (var item in dbList)
        {
            if (id != index)
            {
                index++;
                continue;
            }

            // Remove activity from list
            await Program.Db.HashDeleteAsync("customStatusList", item.Name);
            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent("Activity removed successfully."));
            return;
        }

        // If we're here, the ID wasn't found in the list
        await ctx.FollowupAsync(
            new DiscordFollowupMessageBuilder().WithContent("There's no activity with that ID!"));
    }

    [Command("randomize")]
    [Description("Choose a random custom status message from the list.")]
    public static async Task RandomizeActivity(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        DiscordActivity storedActivity;
        try
        {
            // Get activity from db if stored
            storedActivity = JsonConvert.DeserializeObject<DiscordActivity>(
                await Program.Db.HashGetAsync("customStatus", "activity"));
        }
        catch
        {
            // No activity stored; clear
            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                    "There are no custom status messages in the list! Activity cleared."));
            return;
        }

        // Activity was set with /activity set (see SetActvity method below); do not override
        if (!string.IsNullOrWhiteSpace(storedActivity.Name))
        {
            await Program.Discord.UpdateStatusAsync(storedActivity,
                JsonConvert.DeserializeObject<DiscordUserStatus>(
                    await Program.Db.HashGetAsync("customStatus", "userStatus")));

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"The bot's activity was previously set with {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "set")} and has thus not been changed." +
                $"\nIf you wish to proceed with this command, please first clear the current status with {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "reset")}."));

            return;
        }

        await ActivityTasks.SetActivityAsync();

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("Activity randomized!"));
    }

    [Command("set")]
    [Description("Set the bot's activity. This overrides the list of status messages to cycle through.")]
    private static async Task SetActivity(SlashCommandContext ctx,
        [Parameter("status"), Description("The bot's online status.")]
        [SlashChoiceProvider(typeof(OnlineStatusChoiceProvider))]
        string status,
        [Parameter("type"), Description("The type of status (playing, watching, etc).")]
        [SlashChoiceProvider(typeof(ActivityTypeChoiceProvider))]
        string type = null,
        [Parameter("message"), Description("The message to show after the status type.")] [MinMaxLength(maxLength: 128)]
        string message = null)
    {
        await ctx.DeferResponseAsync();

        var customStatusDisabled = await Program.Db.StringGetAsync("customStatusDisabled");
        if (customStatusDisabled == "true")
        {
            // Custom status messages are disabled; warn user and stop
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Custom status messages are disabled! Use {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "enable")} to enable them first."));
            return;
        }

        DiscordActivity activity = new() { Name = message };

        if (type is null)
            activity.ActivityType = default;

        // Determine activity type
        if (activity.ActivityType != DiscordActivityType.Streaming)
            activity.ActivityType = type?.ToLower() switch
            {
                "playing" => DiscordActivityType.Playing,
                "watching" => DiscordActivityType.Watching,
                "competing" => DiscordActivityType.Competing,
                "listening" => DiscordActivityType.ListeningTo,
                _ => default
            };

        // Determine status type
        var userStatus = status.ToLower() switch
        {
            "online" => DiscordUserStatus.Online,
            "idle" => DiscordUserStatus.Idle,
            "dnd" => DiscordUserStatus.DoNotDisturb,
            "offline" => DiscordUserStatus.Invisible,
            "invisible" => DiscordUserStatus.Invisible,
            _ => DiscordUserStatus.Online
        };

        // Store activity in db
        await Program.Db.HashSetAsync("customStatus", "activity", JsonConvert.SerializeObject(activity));
        await Program.Db.HashSetAsync("customStatus", "userStatus",
            JsonConvert.SerializeObject(userStatus));

        if (activity.Name is not null) await ReplaceActivityKeywords(activity);

        await ctx.Client.UpdateStatusAsync(activity, userStatus);

        await ctx.FollowupAsync(
            new DiscordFollowupMessageBuilder().WithContent("Activity set successfully!"));
    }

    [Command("reset")]
    [Description("Reset the bot's activity; it will cycle through the list of custom status messages.")]
    public static async Task ResetActivity(SlashCommandContext ctx)
    {
        await SetActivity(ctx, "online");
    }

    [Command("disable")]
    [Description("Clear the bot's status and stop it from cycling through the list.")]
    public static async Task DisableActivity(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        // Clear activity
        await Program.Discord.UpdateStatusAsync(new DiscordActivity(), DiscordUserStatus.Online);

        // Set disabled flag so that activity is not set on schedule or with commands until re-enabled
        await Program.Db.StringSetAsync("customStatusDisabled", "true");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
            "Custom status messages disabled. Note that the bot's status may not be cleared right away due to caching."));
    }

    [Command("enable")]
    [Description("Allow the bot to cycle through its list of custom status messages or use one set with /activity set.")]
    public static async Task EnableActivity(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();

        // Clear disabled flag so that activity can be set on schedule or with commands
        await Program.Db.StringSetAsync("customStatusDisabled", "false");

        // Randomize activity
        await ActivityTasks.SetActivityAsync();

        await ctx.FollowupAsync(
            new DiscordFollowupMessageBuilder().WithContent("Custom status messages enabled."));
    }

    /// <summary>
    ///     Parses a string ("playing", "watching", etc.) to a DiscordActivity.ActivityType.
    /// </summary>
    /// <param name="activity">The activity to update the ActivityType for.</param>
    /// <param name="customStatusListItem">The redis hash entry whose Value is the string to parse.</param>
    /// <returns>The DiscordActivity provided with an updated ActivityType.</returns>
    private static (bool successStatus, DiscordActivity activity) ParseActivityType(DiscordActivity activity,
        HashEntry customStatusListItem)
    {
        switch (customStatusListItem.Value.ToString().ToLower())
        {
            case "playing":
                activity.ActivityType = DiscordActivityType.Playing;
                break;
            case "watching":
                activity.ActivityType = DiscordActivityType.Watching;
                break;
            case "listening":
                activity.ActivityType = DiscordActivityType.ListeningTo;
                break;
            case "listening to":
                activity.ActivityType = DiscordActivityType.ListeningTo;
                break;
            case "competing":
                activity.ActivityType = DiscordActivityType.Competing;
                break;
            case "competing in":
                activity.ActivityType = DiscordActivityType.Competing;
                break;
            case "streaming":
                return (false, activity); // activity is unaltered as streaming is not supported currently
            default:
                return (false, activity); // activity is unaltered
        }

        return (true, activity);
    }

    /// <summary>
    ///     Generates an error embed to send if there is an error while trying to set a custom status message.
    ///     Generally these errors are thrown due to an invalid activity type, or if it is set to 'streaming'.
    /// </summary>
    /// <param name="customStatusListItem">
    ///     The redis hash entry whose Value is the string that failed to be parsed as a
    ///     DiscordActivity.
    /// </param>
    /// <param name="activityTypeIsStreaming">Whether the activity type was 'streaming'.</param>
    /// <returns>An embed containing error information to be sent to a log channel.</returns>
    private static DiscordEmbedBuilder GetCustomStatusErrorEmbed(HashEntry customStatusListItem,
        bool activityTypeIsStreaming = false)
    {
        DiscordEmbedBuilder invalidErrorEmbed = new()
        {
            Color = new DiscordColor("FF0000"),
            Title = "An error occurred while processing a custom status message",
            Description = activityTypeIsStreaming
                ? "The activity type \"Streaming\" is not currently supported."
                : "The target activity type was invalid.",
            Timestamp = DateTime.UtcNow
        };
        invalidErrorEmbed.AddField("Custom Status Message", customStatusListItem.Name);
        invalidErrorEmbed.AddField("Target Activity Type", customStatusListItem.Value.ToString());

        return invalidErrorEmbed;
    }

    /// <summary>
    ///     Replaces keywords in activity names (like {uptime}) with the corresponding information (e.g. bot uptime).
    /// </summary>
    /// <param name="activity">The activity whose Name contains keywords to replace with information.</param>
    /// <returns>An updated DiscordActivity with keywords in its Name replaced with the corresponding information.</returns>
    private static async Task<DiscordActivity> ReplaceActivityKeywords(DiscordActivity activity)
    {
        if (activity.Name.Contains("{uptime}"))
        {
            var uptime = DateTime.Now.Subtract(Convert.ToDateTime(Program.ProcessStartTime));
            activity.Name = activity.Name.Replace("{uptime}", uptime.Humanize());
        }

        if (activity.Name.Contains("{userCount}"))
        {
            List<DiscordUser> uniqueUsers = [];
            foreach (var guild in Program.Discord.Guilds)
            foreach (var member in guild.Value.Members)
            {
                var user = await Program.Discord.GetUserAsync(member.Value.Id);
                if (!uniqueUsers.Contains(user)) uniqueUsers.Add(user);
            }

            activity.Name = activity.Name.Replace("{userCount}", uniqueUsers.Count.ToString());
        }

        if (activity.Name.Contains("{serverCount}"))
            activity.Name = activity.Name.Replace("{serverCount}",
                Program.Discord.Guilds.Count.ToString());

        if (activity.Name.Contains("{keywordCount}"))
            activity.Name = activity.Name.Replace("{keywordCount}",
                Program.Db.HashGetAllAsync("keywords").Result.Length.ToString());

        return activity;
    }
    
    private class ActivityTypeChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("Playing", "playing"),
            new("Watching", "watching"),
            new("Competing in", "competing"),
            new("Listening to", "listening")
        ];
        
        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }
    
    private class OnlineStatusChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("Online", "online"),
            new("Idle", "idle"),
            new("Do Not Disturb", "dnd"),
            new("Invisible", "invisible")
        ];
        
        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }
}
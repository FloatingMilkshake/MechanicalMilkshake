namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

public class ActivityCommands : ApplicationCommandModule
{
    [SlashCommandGroup("activity", "Configure the bot's activity!")]
    public class ActivityCmds
    {
        [SlashCommand("add",
            "Add a custom status message to the list that the bot cycles through, or modify an existing entry.")]
        public static async Task AddActivity(InteractionContext ctx,
            [Option("type", "The type of status (playing, watching, etc).")]
            [Choice("Playing", "playing")]
            [Choice("Watching", "watching")]
            [Choice("Competing in", "competing")]
            [Choice("Listening to", "listening")]
            string type,
            [Option("message", "The message to show after the status type.")] [MaximumLength(128)]
            string message)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            await Program.Db.HashSetAsync("customStatusList", message, type);

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("Activity added successfully!"));
        }

        [SlashCommand("list", "List the custom status messages that the bot cycles through.")]
        public static async Task ListActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Get list of custom statuses; if empty, show error & return
            var dbList = await Program.Db.HashGetAllAsync("customStatusList");
            if (dbList.Length == 0)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
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

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(output));
        }

        [SlashCommand("choose", "Choose a custom status message from the list to set now.")]
        public static async Task ChooseActvity(InteractionContext ctx,
            [Option("id", "The ID number of the status to set. You can get this with /activity list.")]
            long id)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

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
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().AddEmbed(GetCustomStatusErrorEmbed(item)));
                    return;
                }

                // Replace keywords like {activity} or {serverCount} with the respective values
                activity = await ReplaceActivityKeywords(activity);

                await Program.Discord.UpdateStatusAsync(activity, UserStatus.Online);
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("Activity updated successfully!"));
                break;
            }
        }

        [SlashCommand("remove", "Remove a custom status message from the list that the bot cycles through.")]
        public static async Task RemoveActivity(InteractionContext ctx,
            [Option("id", "The ID number of the status to remove. You can get this with /activity list.")]
            long id)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var dbList = await Program.Db.HashGetAllAsync("customStatusList");

            if (dbList.Length == 0)
            {
                // List is empty; nothing to remove
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
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
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("Activity removed successfully."));
                return;
            }

            // If we're here, the ID wasn't found in the list
            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("There's no activity with that ID!"));
        }

        [SlashCommand("randomize", "Choose a random custom status message from the list.")]
        public static async Task RandomizeActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

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
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "There are no custom status messages in the list! Activity cleared."));
                return;
            }

            // Activity was set with /activity set (see SetActvity method below); do not override
            if (!string.IsNullOrWhiteSpace(storedActivity.Name))
            {
                await Program.Discord.UpdateStatusAsync(storedActivity,
                    JsonConvert.DeserializeObject<UserStatus>(
                        await Program.Db.HashGetAsync("customStatus", "userStatus")));

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"The bot's activity was previously set with {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "set")} and has thus not been changed." +
                    $"\nIf you wish to proceed with this command, please first clear the current status with {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "reset")}."));

                return;
            }

            await CustomStatusHelpers.SetCustomStatus();

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Activity randomized!"));
        }

        [SlashCommand("set",
            "Set the bot's activity. This overrides the list of status messages to cycle through.")]
        private static async Task SetActivity(InteractionContext ctx,
            [Option("status", "The bot's online status.")]
            [Choice("Online", "online")]
            [Choice("Idle", "idle")]
            [Choice("Do Not Disturb", "dnd")]
            [Choice("Invisible", "invisible")]
            string status,
            [Option("type", "The type of status (playing, watching, etc).")]
            [Choice("Playing", "playing")]
            [Choice("Watching", "watching")]
            [Choice("Competing in", "competing")]
            [Choice("Listening to", "listening")]
            string type = null,
            [Option("message", "The message to show after the status type.")] [MaximumLength(128)]
            string message = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var customStatusDisabled = await Program.Db.StringGetAsync("customStatusDisabled");
            if (customStatusDisabled == "true")
            {
                // Custom status messages are disabled; warn user and stop
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Custom status messages are disabled! Use {SlashCmdMentionHelpers.GetSlashCmdMention("activity", "enable")} to enable them first."));
                return;
            }

            DiscordActivity activity = new() { Name = message };

            if (type is null)
                activity.ActivityType = default;

            // Determine activity type
            if (activity.ActivityType != ActivityType.Streaming)
                activity.ActivityType = type?.ToLower() switch
                {
                    "playing" => ActivityType.Playing,
                    "watching" => ActivityType.Watching,
                    "competing" => ActivityType.Competing,
                    "listening" => ActivityType.ListeningTo,
                    _ => default
                };

            // Determine status type
            var userStatus = status.ToLower() switch
            {
                "online" => UserStatus.Online,
                "idle" => UserStatus.Idle,
                "dnd" => UserStatus.DoNotDisturb,
                "offline" => UserStatus.Invisible,
                "invisible" => UserStatus.Invisible,
                _ => UserStatus.Online
            };

            // Store activity in db
            await Program.Db.HashSetAsync("customStatus", "activity", JsonConvert.SerializeObject(activity));
            await Program.Db.HashSetAsync("customStatus", "userStatus",
                JsonConvert.SerializeObject(userStatus));

            if (activity.Name is not null) await ReplaceActivityKeywords(activity);

            await ctx.Client.UpdateStatusAsync(activity, userStatus);

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("Activity set successfully!"));
        }

        [SlashCommand("reset",
            "Reset the bot's activity; it will cycle through the list of custom status messages.")]
        public static async Task ResetActivity(InteractionContext ctx)
        {
            await SetActivity(ctx, "online");
        }

        [SlashCommand("disable", "Clear the bot's status and stop it from cycling through the list.")]
        public static async Task DisableActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Clear activity
            await Program.Discord.UpdateStatusAsync(new DiscordActivity(), UserStatus.Online);

            // Set disabled flag so that activity is not set on schedule or with commands until re-enabled
            await Program.Db.StringSetAsync("customStatusDisabled", "true");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                "Custom status messages disabled. Note that the bot's status may not be cleared right away due to caching."));
        }

        [SlashCommand("enable",
            "Allow the bot to cycle through its list of custom status messages or use one set with /activity set.")]
        public static async Task EnableActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Clear disabled flag so that activity can be set on schedule or with commands
            await Program.Db.StringSetAsync("customStatusDisabled", "false");

            // Randomize activity
            await CustomStatusHelpers.SetCustomStatus();

            await ctx.FollowUpAsync(
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
                    activity.ActivityType = ActivityType.Playing;
                    break;
                case "watching":
                    activity.ActivityType = ActivityType.Watching;
                    break;
                case "listening":
                    activity.ActivityType = ActivityType.ListeningTo;
                    break;
                case "listening to":
                    activity.ActivityType = ActivityType.ListeningTo;
                    break;
                case "competing":
                    activity.ActivityType = ActivityType.Competing;
                    break;
                case "competing in":
                    activity.ActivityType = ActivityType.Competing;
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
                List<DiscordUser> uniqueUsers = new();
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
    }
}
namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

public class ActivityCommands : ApplicationCommandModule
{
    [SlashCommandGroup("activity", "Configure the bot's activity!")]
    public class ActivityCmds
    {
        [SlashCommand("add",
            "Add a custom status message to the list that the bot cycles through, or modify an existing entry.")]
        public async Task AddActivity(InteractionContext ctx,
            [Option("type", "The type of status (playing, watching, etc).")]
            [Choice("Playing", "playing")]
            [Choice("Watching", "watching")]
            [Choice("Competing in", "competing")]
            [Choice("Listening to", "listening")]
            string type,
            [Option("message", "The message to show after the status type.")]
            string message)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            await Program.db.HashSetAsync("customStatusList", message, type);

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("Activity added successfully!"));
        }

        [SlashCommand("list", "List the custom status messages that the bot cycles through.")]
        public async Task ListActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var dbList = await Program.db.HashGetAllAsync("customStatusList");
            if (dbList.Length == 0)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "There are no custom status messages in the list! Add some with `/activity add`."));
                return;
            }

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
        public async Task ChooseActvity(InteractionContext ctx,
            [Option("id", "The ID number of the status to set. You can get this with /activity list.")]
            long id)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var dbList = await Program.db.HashGetAllAsync("customStatusList");
            var index = 1;
            foreach (var item in dbList)
            {
                if (id == index)
                {
                    DiscordActivity activity = new()
                    {
                        Name = item.Name
                    };
                    string targetActivityType = item.Value;
                    // TODO: make this a helper or something
                    switch (targetActivityType.ToLower())
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
                            DiscordEmbedBuilder streamingErrorEmbed = new()
                            {
                                Color = new DiscordColor("FF0000"),
                                Title = "An error occurred while processing a custom status message",
                                Description = "The activity type \"Streaming\" is not currently supported.",
                                Timestamp = DateTime.UtcNow
                            };
                            streamingErrorEmbed.AddField("Custom Status Message", item.Name);
                            streamingErrorEmbed.AddField("Target Activity Type", targetActivityType);
                            await ctx.FollowUpAsync(
                                new DiscordFollowupMessageBuilder().AddEmbed(streamingErrorEmbed));
                            return;
                        default:
                            DiscordEmbedBuilder invalidErrorEmbed = new()
                            {
                                Color = new DiscordColor("FF0000"),
                                Title = "An error occurred while processing a custom status message",
                                Description = "The target activity type was invalid.",
                                Timestamp = DateTime.UtcNow
                            };
                            invalidErrorEmbed.AddField("Custom Status Message", item.Name);
                            invalidErrorEmbed.AddField("Target Activity Type", targetActivityType);
                            await ctx.FollowUpAsync(
                                new DiscordFollowupMessageBuilder().AddEmbed(invalidErrorEmbed));
                            return;
                    }

                    if (activity.Name.Contains("{uptime}"))
                    {
                        var uptime = DateTime.Now.Subtract(Convert.ToDateTime(Program.processStartTime));
                        activity.Name = activity.Name.Replace("{uptime}", uptime.Humanize());
                    }

                    if (activity.Name.Contains("{userCount}"))
                    {
                        List<DiscordUser> uniqueUsers = new();
                        foreach (var guild in Program.discord.Guilds)
                        foreach (var member in guild.Value.Members)
                        {
                            var user = await Program.discord.GetUserAsync(member.Value.Id);
                            if (!uniqueUsers.Contains(user)) uniqueUsers.Add(user);
                        }

                        activity.Name = activity.Name.Replace("{userCount}", uniqueUsers.Count.ToString());
                    }

                    if (activity.Name.Contains("{serverCount}"))
                        activity.Name = activity.Name.Replace("{serverCount}",
                            Program.discord.Guilds.Count.ToString());

                    await Program.discord.UpdateStatusAsync(activity, UserStatus.Online);
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent("Activity updated successfully!"));
                    break;
                }

                index++;
            }
        }

        [SlashCommand("remove", "Remove a custom status message from the list that the bot cycles through.")]
        public async Task RemoveActivity(InteractionContext ctx,
            [Option("id",
                "The ID number of the status to remove. You can get this with /activity list.")]
            long id)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var dbList = await Program.db.HashGetAllAsync("customStatusList");

            if (dbList.Length == 0)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "There are no custom status messages in the list! Add some with `/activity add`."));
                return;
            }

            var index = 1;
            var itemReached = false;
            foreach (var item in dbList)
            {
                itemReached = false;
                if (id == index)
                {
                    await Program.db.HashDeleteAsync("customStatusList", item.Name);
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent("Activity removed successfully."));
                    return;
                }

                index++;
            }

            if (!itemReached)
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent("There's no activity with that ID!"));
        }

        [SlashCommand("randomize", "Choose a random custom status message from the list.")]
        public async Task RandomizeActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var storedActivity =
                JsonConvert.DeserializeObject<DiscordActivity>(
                    await Program.db.HashGetAsync("customStatus", "activity"));

            if (storedActivity is not null && !string.IsNullOrWhiteSpace(storedActivity.Name))
            {
                await Program.discord.UpdateStatusAsync(storedActivity,
                    JsonConvert.DeserializeObject<UserStatus>(
                        await Program.db.HashGetAsync("customStatus", "userStatus")));

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "The bot's activity was previously set with `/activity set` and has thus not been changed." +
                    "\nIf you wish to proceed with this command, please first clear the current status with `/activity reset`."));

                return;
            }

            await CustomStatusHelper.SetCustomStatus();

            var list = await Program.db.HashGetAllAsync("customStatusList");
            if (list.Length == 0 && Program.discord.CurrentUser.Presence.Activity.Name == null)
            {
                // Activity was cleared; list is empty
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "There are no custom status messages in the list! Activity cleared."));
                return;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Activity randomized!"));
        }

        [SlashCommand("set",
            "Set the bot's activity. This overrides the list of status messages to cycle through.")]
        public async Task SetActivity(InteractionContext ctx,
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
            [Option("message", "The message to show after the status type.")]
            string message = null)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            var customStatusDisabled = await Program.db.StringGetAsync("customStatusDisabled");
            if (customStatusDisabled == "true")
            {
                // Custom status messages are disabled; warn user and don't bother going through with the rest of this command
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Custom status messages are disabled! Use `/activity enable` to enable them first."));
                return;
            }

            DiscordActivity activity = new();
            ActivityType activityType = default;
            activity.Name = message;
            if (type != null)
            {
                if (activityType != ActivityType.Streaming)
                {
                    activityType = type.ToLower() switch
                    {
                        "playing" => ActivityType.Playing,
                        "watching" => ActivityType.Watching,
                        "competing" => ActivityType.Competing,
                        "listening" => ActivityType.ListeningTo,
                        _ => default
                    };
                    activity.ActivityType = activityType;
                }
            }
            else
            {
                activity.ActivityType = default;
            }

            var userStatus = status.ToLower() switch
            {
                "online" => UserStatus.Online,
                "idle" => UserStatus.Idle,
                "dnd" => UserStatus.DoNotDisturb,
                "offline" => UserStatus.Invisible,
                "invisible" => UserStatus.Invisible,
                _ => UserStatus.Online
            };

            await Program.db.HashSetAsync("customStatus", "activity", JsonConvert.SerializeObject(activity));
            await Program.db.HashSetAsync("customStatus", "userStatus",
                JsonConvert.SerializeObject(userStatus));

            if (activity.Name is not null)
            {
                if (activity.Name.Contains("{uptime}"))
                {
                    var uptime = DateTime.Now.Subtract(Convert.ToDateTime(Program.processStartTime));
                    activity.Name = activity.Name.Replace("{uptime}", uptime.Humanize());
                }

                if (activity.Name.Contains("{userCount}"))
                {
                    List<DiscordUser> uniqueUsers = new();
                    foreach (var guild in Program.discord.Guilds)
                    foreach (var member in guild.Value.Members)
                    {
                        var user = await Program.discord.GetUserAsync(member.Value.Id);
                        if (!uniqueUsers.Contains(user)) uniqueUsers.Add(user);
                    }

                    activity.Name = activity.Name.Replace("{userCount}", uniqueUsers.Count.ToString());
                }

                if (activity.Name.Contains("{serverCount}"))
                    activity.Name = activity.Name.Replace("{serverCount}",
                        Program.discord.Guilds.Count.ToString());
            }

            await ctx.Client.UpdateStatusAsync(activity, userStatus);

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("Activity set successfully!"));
        }

        [SlashCommand("reset",
            "Reset the bot's activity; it will cycle through the list of custom status messages.")]
        public async Task ResetActivity(InteractionContext ctx)
        {
            await SetActivity(ctx, "online");
        }

        [SlashCommand("disable", "Clear the bot's status and stop it from cycling through the list.")]
        public async Task DisableActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            await Program.discord.UpdateStatusAsync(new DiscordActivity(), UserStatus.Online);

            await Program.db.StringSetAsync("customStatusDisabled", "true");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                "Custom status messages disabled. Note that the bot's status may not be cleared right away due to caching."));
        }

        [SlashCommand("enable",
            "Allow the bot to cycle through its list of custom status messages or use one set with /activity set.")]
        public async Task EnableActivity(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            await Program.db.StringSetAsync("customStatusDisabled", "false");

            await CustomStatusHelper.SetCustomStatus();

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent("Custom status messages enabled."));
        }
    }
}
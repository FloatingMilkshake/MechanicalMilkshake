namespace MechanicalMilkshake.Tasks;

public class ActivityTasks
{
    public static async Task ExecuteAsync()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromHours(1));
            await SetActivityAsync();
        }
        // ReSharper disable once FunctionNeverReturns
    }
    
    public static async Task SetActivityAsync()
    {
        try
        {
            var currentStatus = await Program.Db.HashGetAsync("customStatus", "activity");

            if (string.IsNullOrWhiteSpace(currentStatus))
            {
                await Program.Db.HashSetAsync("customStatus", "activity",
                    JsonConvert.SerializeObject(new DiscordActivity()));
                await Program.Db.HashSetAsync("customStatus", "userStatus",
                    JsonConvert.SerializeObject(UserStatus.Online));
            }

            currentStatus = await Program.Db.HashGetAsync("customStatus", "activity");

            var currentActivity = JsonConvert.DeserializeObject<DiscordActivity>(currentStatus);

            if (await Program.Db.StringGetAsync("customStatusDisabled") == "true")
            {
                // Custom status disabled! Do not set one. Clear if set.
                DiscordActivity emptyActivity = new();
                await Program.Discord.UpdateStatusAsync(emptyActivity, UserStatus.Online);
                return;
            }

            if (currentActivity!.Name is null && currentActivity!.ActivityType is 0)
            {
                // No custom status set. Pick random from list.
                Random random = new();
                var customStatusList = await Program.Db.HashGetAllAsync("customStatusList");

                // List is empty, don't set a custom status. Clear if set.
                if (customStatusList.Length == 0)
                {
                    DiscordActivity emptyActivity = new();
                    await Program.Discord.UpdateStatusAsync(emptyActivity, UserStatus.Online);
                    return;
                }

                var chosenStatus = random.Next(0, customStatusList.Length);
                if (Program.Discord.CurrentUser.Presence.Activity.Name is not null)
                    if (customStatusList.Length != 1)
                        while (customStatusList[chosenStatus].Name.ToString() ==
                               Program.Discord.CurrentUser.Presence.Activity.Name)
                        {
                            // Don't re-use the same activity! Pick another one.
                            customStatusList = await Program.Db.HashGetAllAsync("customStatusList");
                            chosenStatus = random.Next(0, customStatusList.Length);
                        }

                var activityName = customStatusList[chosenStatus].Name.ToString();

                if (activityName.Contains("{uptime}"))
                {
                    var uptime = DateTime.Now.Subtract(Convert.ToDateTime(Program.ProcessStartTime));

                    // Don't set a custom status message containing {activity} if uptime is less than 1 hour.
                    if (uptime.CompareTo(DateTime.Now.AddHours(-1).TimeOfDay) < 0)
                    {
                        if (customStatusList.Length == 1)
                        {
                            await Program.Discord.UpdateStatusAsync(new DiscordActivity(), UserStatus.Online);
                            return;
                        }

                        while (customStatusList[chosenStatus].Name.ToString().Contains("{uptime}"))
                        {
                            customStatusList = await Program.Db.HashGetAllAsync("customStatusList");
                            chosenStatus = random.Next(0, customStatusList.Length);
                        }

                        activityName = customStatusList[chosenStatus].Name.ToString();
                    }

                    activityName = activityName.Replace("{uptime}", uptime.Humanize());
                }

                if (activityName.Contains("{serverCount}"))
                    activityName = activityName.Replace("{serverCount}", Program.Discord.Guilds.Count.ToString());

                if (activityName.Contains("{keywordCount}"))
                    activityName = activityName.Replace("{keywordCount}",
                        Program.Db.HashGetAllAsync("keywords").Result.Length.ToString());

                DiscordActivity activity = new()
                {
                    Name = activityName
                };
                const UserStatus userStatus = UserStatus.Online;

                string targetActivityType = customStatusList[chosenStatus].Value;
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
                        streamingErrorEmbed.AddField("Custom Status Message", customStatusList[chosenStatus].Name);
                        streamingErrorEmbed.AddField("Target Activity Type", targetActivityType);
                        await Program.HomeChannel.SendMessageAsync(streamingErrorEmbed);
                        return;
                    default:
                        DiscordEmbedBuilder invalidErrorEmbed = new()
                        {
                            Color = new DiscordColor("FF0000"),
                            Title = "An error occurred while processing a custom status message",
                            Description = "The target activity type was invalid.",
                            Timestamp = DateTime.UtcNow
                        };
                        invalidErrorEmbed.AddField("Custom Status Message", customStatusList[chosenStatus].Name);
                        invalidErrorEmbed.AddField("Target Activity Type", targetActivityType);
                        await Program.HomeChannel.SendMessageAsync(invalidErrorEmbed);
                        return;
                }

                if (string.IsNullOrWhiteSpace(Program.Discord.CurrentUser.Presence.Activity.Name))
                    await Program.Db.StringSetAsync("customStatusLastUpdated", $"{DateTime.Now}");
                else
                    await Program.Db.StringSetAsync("customStatusLastUpdated",
                        $"{DateTime.Now}\nPrevious Status: {Program.Discord.CurrentUser.Presence.Activity.ActivityType} {Program.Discord.CurrentUser.Presence.Activity.Name}");

                await Program.Discord.UpdateStatusAsync(activity, userStatus);
            }
            else
            {
                // Restore custom status from db
                var activity =
                    JsonConvert.DeserializeObject<DiscordActivity>(
                        await Program.Db.HashGetAsync("customStatus", "activity"));
                var userStatus =
                    JsonConvert.DeserializeObject<UserStatus>(
                        await Program.Db.HashGetAsync("customStatus", "userStatus"));
                await Program.Discord.UpdateStatusAsync(activity, userStatus);
            }
        }
        catch (Exception ex)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "An exception occurred while processing a custom status message",
                Timestamp = DateTime.UtcNow
            };
            embed.AddField("Message", ex.Message);

            Program.Discord.Logger.LogError(Program.BotEventId, "An exception occurred when processing a custom status message!"
                + "\n{exType}: {exMessage}\n{exStackTrace}", ex.GetType(), ex.Message, ex.StackTrace);

            await Program.HomeChannel.SendMessageAsync(embed);
        }
    }
}
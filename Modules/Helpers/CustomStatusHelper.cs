namespace MechanicalMilkshake.Modules.Helpers;

public class CustomStatusHelper
{
    public static async Task SetCustomStatus()
    {
        try
        {
            var currentStatus = await Program.db.HashGetAsync("customStatus", "activity");
            var currentActivity = JsonConvert.DeserializeObject<DiscordActivity>(currentStatus);

            if (await Program.db.StringGetAsync("customStatusDisabled") == "true")
            {
                // Custom status disabled! Do not set one. Clear if set.
                DiscordActivity emptyActivity = new();
                await Program.discord.UpdateStatusAsync(emptyActivity, UserStatus.Online);
                return;
            }

            if (currentActivity.Name is null && currentActivity.ActivityType is 0)
            {
                // No custom status set. Pick random from list.
                Random random = new();
                var customStatusList = await Program.db.HashGetAllAsync("customStatusList");

                // List is empty, don't set a custom status. Clear if set.
                if (customStatusList.Length == 0)
                {
                    DiscordActivity emptyActivity = new();
                    await Program.discord.UpdateStatusAsync(emptyActivity, UserStatus.Online);
                    return;
                }

                var chosenStatus = random.Next(0, customStatusList.Length);
                if (Program.discord.CurrentUser.Presence.Activity.Name != null)
                    if (customStatusList.Length != 1)
                        while (customStatusList[chosenStatus].Name.ToString() ==
                               Program.discord.CurrentUser.Presence.Activity.Name)
                        {
                            // Don't re-use the same activity! Pick another one.
                            customStatusList = await Program.db.HashGetAllAsync("customStatusList");
                            chosenStatus = random.Next(0, customStatusList.Length);
                        }

                string activityName;
                activityName = customStatusList[chosenStatus].Name.ToString();

                if (activityName.Contains("{uptime}"))
                {
                    var uptime = DateTime.Now.Subtract(Convert.ToDateTime(Program.processStartTime));

                    // Don't set a custom status message containing {activity} if uptime is less than 1 hour.
                    if (uptime.CompareTo(DateTime.Now.AddHours(-1).TimeOfDay) < 0)
                    {
                        if (customStatusList.Length == 1)
                        {
                            await Program.discord.UpdateStatusAsync(new DiscordActivity(), UserStatus.Online);
                            return;
                        }

                        while (customStatusList[chosenStatus].Name.ToString().Contains("{uptime}"))
                        {
                            customStatusList = await Program.db.HashGetAllAsync("customStatusList");
                            chosenStatus = random.Next(0, customStatusList.Length);
                        }
                    }

                    activityName = activityName.Replace("{uptime}", uptime.Humanize());
                }

                if (activityName.Contains("{userCount}"))
                {
                    List<DiscordUser> uniqueUsers = new();
                    foreach (var guild in Program.discord.Guilds)
                    foreach (var member in guild.Value.Members)
                    {
                        var user = await Program.discord.GetUserAsync(member.Value.Id);
                        if (!uniqueUsers.Contains(user)) uniqueUsers.Add(user);
                    }

                    activityName = activityName.Replace("{userCount}", uniqueUsers.Count.ToString());
                }

                if (activityName.Contains("{serverCount}"))
                    activityName = activityName.Replace("{serverCount}", Program.discord.Guilds.Count.ToString());

                DiscordActivity activity = new()
                {
                    Name = activityName
                };
                var userStatus = UserStatus.Online;

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
                        await Program.homeChannel.SendMessageAsync(streamingErrorEmbed);
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
                        await Program.homeChannel.SendMessageAsync(invalidErrorEmbed);
                        return;
                }

                await Program.discord.UpdateStatusAsync(activity, userStatus);
                await Program.db.StringSetAsync("customStatusLastUpdated", $"{DateTime.Now}");
            }
            else
            {
                // Restore custom status from db
                var activity =
                    JsonConvert.DeserializeObject<DiscordActivity>(
                        await Program.db.HashGetAsync("customStatus", "activity"));
                var userStatus =
                    JsonConvert.DeserializeObject<UserStatus>(
                        await Program.db.HashGetAsync("customStatus", "userStatus"));
                await Program.discord.UpdateStatusAsync(activity, userStatus);
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

            await Program.homeChannel.SendMessageAsync(embed);
        }
    }
}
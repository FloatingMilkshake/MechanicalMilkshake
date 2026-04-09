namespace MechanicalMilkshake.Helpers;

internal class ReminderHelpers
{
    internal static async Task LogReminderErrorAsync(DiscordChannel logChannel, Exception ex)
    {
        DiscordEmbedBuilder errorEmbed = new()
        {
            Color = DiscordColor.Red,
            Title = "An exception occurred when checking reminders",
            Description =
                $"`{ex.GetType()}` occurred when checking for overdue reminders."
        };
        errorEmbed.AddField("Message", $"{ex.Message}");
        errorEmbed.AddField("Stack Trace", $"```\n{ex.StackTrace}\n```");

        Setup.State.Discord.Client.Logger.LogError("An exception occurred when checking reminders!"
            + "\n{exType}: {exMessage}\n{exStackTrace}", ex.GetType(), ex.Message, ex.StackTrace);

        await logChannel.SendMessageAsync(errorEmbed);
    }

    internal static void AddReminderDelayEmbedField(DiscordEmbedBuilder embed, ulong msgId = default)
    {
        var id = msgId == default ? "[loading...]" : $"`{msgId}`";

        embed.AddField("Need to delay this reminder?",
            $"Use {CommandHelpers.GetSlashCmdMention("reminder delay")} and set `message` to {id}.");
    }

    internal static async Task<(DateTime? parsedTime, string error)> ValidateReminderTriggerTimeAsync(string triggerTime)
    {
        if (!DateTime.TryParse(triggerTime, out DateTime parsedTime))
        {
            try
            {
                parsedTime = HumanDateParser.HumanDateParser.Parse(triggerTime);
            }
            catch (ParseException)
            {
                return (null, $"I couldn't parse \"{triggerTime}\" as a time! Please try again.");
            }
        }

        if (parsedTime <= DateTime.Now)
        {
            return (null, "You can't set a reminder for a time in the past!");
        }

        return (parsedTime, null);
    }

    internal static async Task<(Setup.Types.Reminder reminder, string error)> GetReminderAsync(string reminderId, ulong requestingUserId)
    {
        if (!Setup.Constants.RegularExpressions.DiscordIdPattern.IsMatch(reminderId))
            return (null, "The reminder ID you provided isn't correct! It should look something like this: `1234`." +
                $" You can see your reminders and their IDs with {CommandHelpers.GetSlashCmdMention("reminder list")}.");

        Setup.Types.Reminder reminder;
        try
        {
            reminder = JsonConvert.DeserializeObject<Setup.Types.Reminder>(await Setup.Storage.Redis.HashGetAsync("reminders", reminderId));
        }
        catch
        {
            return (null, "I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`." +
                $" You can see your reminders and their IDs with {CommandHelpers.GetSlashCmdMention("reminder list")}.");
        }

        if (reminder.UserId != requestingUserId)
            return (null, "I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`." +
                $" You can see your reminders and their IDs with {CommandHelpers.GetSlashCmdMention("reminder list")}.");

        return (reminder, null);
    }

    internal static async Task<List<Setup.Types.Reminder>> GetUserRemindersAsync(ulong userId)
    {
        return (await Setup.Storage.Redis.HashGetAllAsync("reminders"))
            .Select(x => JsonConvert.DeserializeObject<Setup.Types.Reminder>(x.Value)).Where(r => r is not null && r.UserId == userId)
            .OrderBy(x => x.TriggerTime)
            .ToList();
    }

    internal static async Task<int> GenerateUniqueReminderIdAsync()
    {
        Random random = new();
        var reminderId = random.Next(1000, 9999);

        var reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");
        while (reminders.Any(x => x.Name == reminderId))
            reminderId = random.Next(1000, 9999);

        return reminderId;
    }

    internal static DiscordSelectComponent CreateSelectComponentFromReminders(List<Setup.Types.Reminder> reminders, string componentCustomId)
    {
        List<DiscordSelectComponentOption> options = reminders.Select(reminder =>
            new DiscordSelectComponentOption(string.IsNullOrWhiteSpace(reminder.ReminderText)
                ? "[no content]"
                : reminder.ReminderText.Truncate(100),
                reminder.ReminderId.ToString(),
                reminder.TriggerTime.Humanize()))
            .ToList();

        return new DiscordSelectComponent(componentCustomId, null, options);
    }

    internal static async Task<DiscordEmbed> CreateReminderListEmbedAsync(List<Setup.Types.Reminder> reminders)
    {
        string embedDescription = "";
        foreach (var reminder in reminders)
        {
            var reminderSetTimeTimestamp = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();
            var reminderTriggerTimeTimestamp = ((DateTimeOffset)reminder.TriggerTime).ToUnixTimeSeconds();

            string guildName;
            if (Setup.Constants.RegularExpressions.DiscordIdPattern.IsMatch(reminder.GuildId))
            {
                var targetGuild = Setup.State.Discord.Client.Guilds[Convert.ToUInt64(reminder.GuildId)];
                guildName = targetGuild.Name;
            }
            else
            {
                guildName = "DMs";
            }

            var reminderLink = $"<https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}>";

            var reminderText = reminder.ReminderText.Truncate(350, " *(truncated)*");

            if (guildName != "DMs")
                guildName += $" <#{reminder.ChannelId}>";

            embedDescription += $"`{reminder.ReminderId}`:\n"
                      + (string.IsNullOrWhiteSpace(reminderText)
                          ? ""
                          : $"> {reminderText}\n")
                      + $"[Set <t:{reminderSetTimeTimestamp}:R>]({reminderLink}) to remind you <t:{reminderTriggerTimeTimestamp}:R> in {guildName}\n\n";
        }

        DiscordEmbedBuilder embed = new()
        {
            Title = "Reminders",
            Color = Setup.Constants.BotColor
        };

        if (embedDescription.Length > 4096)
        {
            embed.WithColor(DiscordColor.Red);

            var embedDescriptionWithTruncatedReminders = $"You have too many reminders to list here! Here are the IDs of each one. Use {CommandHelpers.GetSlashCmdMention("reminder show")} for details.\n";

            foreach (var reminder in reminders)
            {
                var reminderSetTimeTimestamp = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();
                long reminderTriggerTimeTimestamp = ((DateTimeOffset)reminder.TriggerTime).ToUnixTimeSeconds();
                embedDescriptionWithTruncatedReminders += $"\n`{reminder.ReminderId}` - set <t:{reminderSetTimeTimestamp}:R> to remind you <t:{reminderTriggerTimeTimestamp}:R>";
            }

            embed.WithDescription(embedDescriptionWithTruncatedReminders);
        }
        else
        {
            embed.WithDescription(embedDescription);
        }

        return embed;
    }
}

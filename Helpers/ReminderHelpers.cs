namespace MechanicalMilkshake.Helpers;

public partial class ReminderHelpers
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

        Program.Discord.Logger.LogError(Program.BotEventId, "An exception occurred when checking reminders!"
            + "\n{exType}: {exMessage}\n{exStackTrace}", ex.GetType(), ex.Message, ex.StackTrace);

        await logChannel.SendMessageAsync(errorEmbed);
    }

    internal static void AddReminderDelayEmbedField(DiscordEmbedBuilder embed, ulong msgId = default)
    {
        var id = msgId == default ? "[loading...]" : $"`{msgId}`";

        embed.AddField("Need to delay this reminder?",
            $"Use {SlashCmdMentionHelpers.GetSlashCmdMention("reminder", "delay")} and set `message` to {id}.");
    }

    internal static async Task<(DateTime? parsedTime, string error)> ValidateReminderTimeAsync(string reminderTime)
    {
        if (!DateTime.TryParse(reminderTime, out DateTime parsedTime))
        {
            try
            {
                parsedTime = HumanDateParser.HumanDateParser.Parse(reminderTime);
            }
            catch (ParseException)
            {
                return (null, $"I couldn't parse \"{reminderTime}\" as a time! Please try again.");
            }
        }

        if (parsedTime <= DateTime.Now)
        {
            // If user says something like "4pm" and its past 4pm, assume they mean "4pm tomorrow"
            if (parsedTime.Date == DateTime.Now.Date &&
                parsedTime.TimeOfDay < DateTime.Now.TimeOfDay)
            {
                parsedTime = parsedTime.AddDays(1);
            }
            else
            {
                return (null, "You can't set a reminder for a time in the past!");
            }
        }

        return (parsedTime, null);
    }

    internal static async Task<(Reminder reminder, string error)> GetReminderAsync(string reminderId, ulong requestingUserId)
    {
        var reminderCmd = Program.ApplicationCommands.FirstOrDefault(x => x.Name == "reminder");

        if (!IdPattern().IsMatch(reminderId))
            return (null, $"The reminder ID you provided isn't correct! It should look something like this: `1234`.{(reminderCmd is null ? "" : $" You can see your reminders and their IDs with </reminder list:{reminderCmd.Id}>.")}");

        Reminder reminder;
        try
        {
            reminder = JsonConvert.DeserializeObject<Reminder>(await Program.Db.HashGetAsync("reminders", reminderId));
        }
        catch
        {
            return (null, $"I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`.{(reminderCmd is null ? "" : $" You can see your reminders and their IDs with </reminder list:{reminderCmd.Id}>.")}");
        }

        if (reminder.UserId != requestingUserId)
            return (null, $"I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`.{(reminderCmd is null ? "" : $" You can see your reminders and their IDs with </reminder list:{reminderCmd.Id}>.")}");

        return (reminder, null);
    }

    internal static async Task<List<Reminder>> GetUserRemindersAsync(ulong userId)
    {
        return (await Program.Db.HashGetAllAsync("reminders"))
            .Select(x => JsonConvert.DeserializeObject<Reminder>(x.Value)).Where(r => r is not null && r.UserId == userId)
            .OrderBy(x => x.ReminderTime)
            .ToList();
    }

    internal static async Task<int> GenerateUniqueReminderIdAsync()
    {
        Random random = new();
        var reminderId = random.Next(1000, 9999);

        var reminders = await Program.Db.HashGetAllAsync("reminders");
        while (reminders.Any(x => x.Name == reminderId))
            reminderId = random.Next(1000, 9999);

        return reminderId;
    }

    internal static DiscordSelectComponent CreateSelectComponentFromReminders(List<Reminder> reminders, string componentCustomId)
    {
        List<DiscordSelectComponentOption> options = reminders.Select(reminder =>
            new DiscordSelectComponentOption(reminder.ReminderText.Truncate(100),
                reminder.ReminderId.ToString(),
                reminder.ReminderTime.Humanize()))
            .ToList();

        return new DiscordSelectComponent(componentCustomId, null, options);
    }

    internal static async Task<DiscordEmbed> CreateReminderListEmbedAsync(List<Reminder> reminders)
    {
        string output = "";
        foreach (var reminder in reminders)
        {
            var setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();

            long reminderTime = default;
            if (reminder.ReminderTime is not null)
                reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

            var idRegex = IdPattern();
            string guildName;
            if (idRegex.IsMatch(reminder.GuildId))
            {
                var targetGuild = await Program.Discord.GetGuildAsync(Convert.ToUInt64(reminder.GuildId));
                guildName = targetGuild.Name;
            }
            else
            {
                guildName = "DMs";
            }

            var reminderLink = $"<https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}>";

            var reminderText = reminder.ReminderText.Length > 350
                ? $"{reminder.ReminderText.Truncate(350)} *(truncated)*"
                : reminder.ReminderText;

            var reminderLocation = $" in {guildName}";
            if (guildName != "DMs")
                reminderLocation += $" <#{reminder.ChannelId}>";

            output += $"`{reminder.ReminderId}`:\n"
                      + (string.IsNullOrWhiteSpace(reminderText)
                          ? ""
                          : $"> {reminderText}\n")
                      + (reminder.ReminderTime is null
                          ? reminder.IsPrivate
                              ? $"[Set <t:{setTime}:R>]({reminderLink}). This reminder will not be sent automatically."
                                  + " This reminder was set privately, so this is only a link to the messages around the time it was set."
                              : $"[Set <t:{setTime}:R>]({reminderLink}). This reminder will not be sent automatically."
                          : $"[Set <t:{setTime}:R>]({reminderLink}) to remind you <t:{reminderTime}:R>");

            if (reminder.ReminderTime is not null)
                output += reminderLocation;

            output += "\n\n";
        }

        DiscordEmbedBuilder embed = new()
        {
            Title = "Reminders",
            Color = Program.BotColor
        };

        if (output.Length > 4096)
        {
            embed.WithColor(DiscordColor.Red);

            var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");
            var desc = reminderCmd is null
                    ? "You have too many reminders to list here! Here are the IDs of each one.\n\n"
                    : $"You have too many reminders to list here! Here are the IDs of each one. Use </{reminderCmd.Name} show:{reminderCmd.Id}> for details.\n\n";

            foreach (var reminder in reminders)
            {
                var setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();

                long reminderTime = default;
                if (reminder.ReminderTime is not null)
                    reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

                desc += reminder.ReminderTime is null
                    ? $"`{reminder.ReminderId}` - set <t:{setTime}:R>. This reminder will not be sent automatically."
                    : $"`{reminder.ReminderId}` - set <t:{setTime}:R> to remind you <t:{reminderTime}:R>\n";
            }

            embed.WithDescription(desc.Trim());
        }
        else
        {
            embed.WithDescription(output);
        }

        return embed;
    }

    [GeneratedRegex("[0-9]+")]
    internal static partial Regex IdPattern();
}
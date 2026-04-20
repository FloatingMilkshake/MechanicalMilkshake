namespace MechanicalMilkshake.Setup.Types;

internal sealed class Reminder
{
    [JsonProperty("userId")] internal ulong UserId { get; private set; }

    [JsonProperty("channelId")] internal ulong ChannelId { get; private set; }

    [JsonProperty("guildId")] internal string GuildId { get; private set; }

    [JsonProperty("messageId")] internal ulong MessageId { get; private set; }

    [JsonProperty("reminderId")] internal int ReminderId { get; private set; }

    [JsonProperty("reminderText")] internal string ReminderText { get; private set; }

    [JsonProperty("reminderTime")] internal DateTime TriggerTime { get; private set; }

    [JsonProperty("setTime")] internal DateTime SetTime { get; private set; }

    internal Reminder(ulong userId, ulong channelId, string guildId, ulong messageId, int reminderId,
        string reminderText, DateTime triggerTime, DateTime setTime)
    {
        UserId = userId;
        ChannelId = channelId;
        GuildId = guildId;
        MessageId = messageId;
        ReminderId = reminderId;
        ReminderText = reminderText;
        TriggerTime = triggerTime;
        SetTime = setTime;
    }

    internal long GetTriggerTimeTimestamp()
    {
        return TriggerTime.ToUnixTimeSeconds();
    }

    internal long GetSetTimeTimestamp()
    {
        return SetTime.ToUnixTimeSeconds();
    }

    internal string GetJumpLink()
    {
        return $"https://discord.com/channels/{GuildId}/{ChannelId}/{MessageId}";
    }

    internal DiscordEmbedBuilder CreateEmbed()
    {
        var reminderEmbed = new DiscordEmbedBuilder()
        {
            Color = new DiscordColor("#7287fd"),
            Title = $"Reminder from <t:{GetSetTimeTimestamp()}:R>",
            Description = ReminderText
        };

        string context;
        if (GuildId == "@me")
        {
            context = "This reminder was set privately, so I can't link back to the message where it was set!" +
                $" However, [this link]({GetJumpLink()}) should show you messages around the time that you set the reminder.";
        }
        else
        {
            context = GetJumpLink();
        }
        reminderEmbed.AddField("Context", context);

        return reminderEmbed;
    }

    internal static (DateTime? parsedTime, string error) ParseTriggerTime(string triggerTime)
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
        if (!Setup.Constants.RegularExpressions.ReminderIdPattern.IsMatch(reminderId))
            return (null, "The reminder ID you provided isn't correct! It should look something like this: `1234`." +
                $" You can see your reminders and their IDs with {"reminder list".AsSlashCommandMention()}.");

        Setup.Types.Reminder reminder;
        try
        {
            reminder = JsonConvert.DeserializeObject<Setup.Types.Reminder>(await Setup.Storage.Redis.HashGetAsync("reminders", reminderId));
        }
        catch (Exception ex) when (ex is ArgumentNullException or JsonReaderException)
        {
            return (null, "I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`." +
                $" You can see your reminders and their IDs with {"reminder list".AsSlashCommandMention()}.");
        }

        if (reminder.UserId != requestingUserId)
            return (null, "I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`." +
                $" You can see your reminders and their IDs with {"reminder list".AsSlashCommandMention()}.");

        return (reminder, null);
    }

    internal static async Task<List<Setup.Types.Reminder>> GetUserRemindersAsync(ulong userId)
    {
        return (await Setup.Storage.Redis.HashGetAllAsync("reminders"))
            .Select(x => JsonConvert.DeserializeObject<Setup.Types.Reminder>(x.Value)).Where(r => r is not null && r.UserId == userId)
            .OrderBy(x => x.TriggerTime)
            .ToList();
    }

    internal static async Task<int> GenerateUniqueIdAsync()
    {
        Random random = new();
        var reminderId = random.Next(1000, 9999);

        var reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");
        while (reminders.Any(x => x.Name == reminderId))
            reminderId = random.Next(1000, 9999);

        return reminderId;
    }
}

namespace MechanicalMilkshake.Entities;

public class Reminder
{
    [JsonProperty("userId")] public ulong UserId { get; set; }

    [JsonProperty("channelId")] public ulong ChannelId { get; set; }

    [JsonProperty("guildId")] public string GuildId { get; set; }

    [JsonProperty("messageId")] public ulong MessageId { get; set; }

    [JsonProperty("reminderId")] public int ReminderId { get; set; }

    [JsonProperty("reminderText")] public string ReminderText { get; set; }

    [JsonProperty("reminderTime")] public DateTime? ReminderTime { get; set; }

    [JsonProperty("setTime")] public DateTime SetTime { get; set; }

    [JsonProperty("isPrivate")] public bool IsPrivate { get; set; }
}
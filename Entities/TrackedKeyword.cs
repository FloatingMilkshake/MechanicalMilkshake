namespace MechanicalMilkshake.Entities;

public class TrackedKeyword
{
    [JsonProperty("keyword")] public string Keyword { get; set; }

    [JsonProperty("userId")] public ulong UserId { get; set; }

    [JsonProperty("matchWholeWord")] public bool MatchWholeWord { get; set; }

    [JsonProperty("ignoreBots")] public bool IgnoreBots { get; set; }

    [JsonProperty("assumePresence")] public bool AssumePresence { get; set; }

    [JsonProperty("userIgnoreList")] public List<ulong> UserIgnoreList { get; set; }

    [JsonProperty("channelIgnoreList")] public List<ulong> ChannelIgnoreList { get; set; }

    [JsonProperty("guildIgnoreList")] public List<ulong> GuildIgnoreList { get; set; }

    [JsonProperty("id")] public ulong Id { get; set; }

    [JsonProperty("guildId")] public ulong GuildId { get; set; }
}
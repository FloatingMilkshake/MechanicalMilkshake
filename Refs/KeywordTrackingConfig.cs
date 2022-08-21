namespace MechanicalMilkshake.Refs;

public class KeywordConfig
{
    [JsonProperty("keyword")] public string Keyword { get; set; }

    [JsonProperty("userId")] public ulong UserId { get; set; }

    [JsonProperty("matchWholeWord")] public bool MatchWholeWord { get; set; }

    [JsonProperty("ignoreBots")] public bool IgnoreBots { get; set; }

    [JsonProperty("ignoreList")] public List<ulong> IgnoreList { get; set; }

    [JsonProperty("id")] public ulong Id { get; set; }
}
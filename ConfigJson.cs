namespace MechanicalMilkshake;

public class ConfigJson
{
    [JsonProperty("botToken")] public string BotToken { get; private set; }

    [JsonProperty("homeChannel")] public string HomeChannel { get; private set; }

    [JsonProperty("homeServer")] public string HomeServer { get; private set; }
    
    [JsonProperty("hastebinUrl")] public string HastebinUrl { get; private set; }

    [JsonProperty("wolframAlphaAppId")] public string WolframAlphaAppId { get; private set; }

    [JsonProperty("botCommanders")] public string[] BotCommanders { get; private set; }

    [JsonProperty("useServerSpecificFeatures")] public bool UseServerSpecificFeatures { get; private set; }
    
    [JsonProperty("uptimeKumaHeartbeatUrl")] public string UptimeKumaHeartbeatUrl { get; private set; }
    
    [JsonProperty("feedbackChannel")] public string FeedbackChannel { get; private set; }
    
    [JsonProperty("ratelimitCautionChannels")] public List<string> RatelimitCautionChannels { get; private set; }

    [JsonProperty("slashCommandLogChannel")] public string SlashCommandLogChannel { get; set; }
    
    [JsonProperty("slashCommandLogExcludedGuilds")] public string[] SlashCommandLogExcludedGuilds { get; set; }
    
    [JsonProperty("guildLogChannel")] public string GuildLogChannel { get; private set; }

    [JsonProperty("doDbotsStatsPosting")] public bool DoDbotsStatsPosting { get; private set; }
    
    [JsonProperty("dbotsApiToken")] public string DbotsApiToken { get; private set; }
}
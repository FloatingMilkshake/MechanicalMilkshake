namespace MechanicalMilkshake.Setup.Types;

internal sealed class ConfigJson
{
    [JsonProperty("botToken")] internal string BotToken { get; private set; }

    [JsonProperty("homeChannel")] internal string HomeChannel { get; private set; }

    [JsonProperty("homeServer")] internal string HomeServer { get; private set; }

    [JsonProperty("wolframAlphaAppId")] internal string WolframAlphaAppId { get; private set; }

    [JsonProperty("botCommanders")] internal string[] BotCommanders { get; private set; }

    [JsonProperty("useServerSpecificFeatures")] internal bool UseServerSpecificFeatures { get; private set; }

    [JsonProperty("uptimeKumaHeartbeatUrl")] internal string UptimeKumaHeartbeatUrl { get; private set; }

    [JsonProperty("feedbackChannel")] internal string FeedbackChannel { get; private set; }

    [JsonProperty("ratelimitCautionChannels")] internal List<string> RatelimitCautionChannels { get; private set; }

    [JsonProperty("slashCommandLogChannel")] internal string SlashCommandLogChannel { get; set; }

    [JsonProperty("slashCommandLogExcludedGuilds")] internal string[] SlashCommandLogExcludedGuilds { get; set; }

    [JsonProperty("guildLogChannel")] internal string GuildLogChannel { get; private set; }

    [JsonProperty("doDbotsStatsPosting")] internal bool DoDbotsStatsPosting { get; private set; }

    [JsonProperty("dbotsApiToken")] internal string DbotsApiToken { get; private set; }
}

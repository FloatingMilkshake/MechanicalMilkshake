namespace MechanicalMilkshake;

public class ConfigJson
{
    [JsonProperty("base")] public BaseConfig Base { get; private set; }
    
    [JsonProperty("ids")] public IdsConfig Ids { get; private set; }

    [JsonProperty("workerLinks")] public WorkerLinksConfig WorkerLinks { get; private set; }

    [JsonProperty("s3")] public S3Config S3 { get; private set; }

    [JsonProperty("cloudflare")] public CloudflareConfig Cloudflare { get; private set; }

    [JsonProperty("logs")] public LogConfig Logs { get; private set; }
    
    [JsonProperty("dbots")] public DBotsConfig DBots { get; private set; }
}

public class BaseConfig
{
    [JsonProperty("botToken")] public string BotToken { get; private set; }

    [JsonProperty("homeChannel")] public string HomeChannel { get; private set; }

    [JsonProperty("homeServer")] public string HomeServer { get; private set; }
    
    [JsonProperty("hastebinUrl")] public string HastebinUrl { get; private set; }

    [JsonProperty("wolframAlphaAppId")] public string WolframAlphaAppId { get; private set; }

    [JsonProperty("authorizedUsers")] public string[] AuthorizedUsers { get; private set; }

    [JsonProperty("sshHosts")] public string[] SshHosts { get; private set; }

    [JsonProperty("useServerSpecificFeatures")] public bool UseServerSpecificFeatures { get; private set; }
    
    [JsonProperty("uptimeKumaHeartbeatUrl")] public string UptimeKumaHeartbeatUrl { get; private set; }
}

public class IdsConfig
{
    [JsonProperty("feedbackChannel")] public string FeedbackChannel { get; private set; }
    
    [JsonProperty("ratelimitCautionChannels")] public List<string> RatelimitCautionChannels { get; private set; }
}

public class WorkerLinksConfig
{
    [JsonProperty("baseUrl")] public string BaseUrl { get; set; }

    [JsonProperty("secret")] public string Secret { get; set; }

    [JsonProperty("namespaceId")] public string NamespaceId { get; set; }

    [JsonProperty("apiKey")] public string ApiKey { get; set; }

    [JsonProperty("accountId")] public string AccountId { get; set; }

    [JsonProperty("email")] public string Email { get; set; }
}

public class S3Config
{
    [JsonProperty("bucket")] public string Bucket { get; set; }

    [JsonProperty("cdnBaseUrl")] public string CdnBaseUrl { get; set; }

    [JsonProperty("endpoint")] public string Endpoint { get; set; }

    [JsonProperty("accessKey")] public string AccessKey { get; set; }

    [JsonProperty("secretKey")] public string SecretKey { get; set; }

    [JsonProperty("region")] public string Region { get; set; }
}

public class CloudflareConfig
{
    [JsonProperty("urlPrefix")] public string UrlPrefix { get; set; }

    [JsonProperty("zoneId")] public string ZoneId { get; set; }

    [JsonProperty("token")] public string Token { get; set; }
}

public class LogConfig
{
    [JsonProperty("slashCommands")] public SlashCommandLogConfig SlashCommands { get; private set; }
    
    [JsonProperty("guilds")] public string Guilds { get; private set; }
    
    public class SlashCommandLogConfig
    {
        [JsonProperty("logChannel")] public string LogChannel { get; set; }
        [JsonProperty("cmdLogExcludedGuilds")] public string[] CmdLogExcludedGuilds { get; set; }
    }
}

public class DBotsConfig
{
    [JsonProperty("doStatsPosting")] public bool DoStatsPosting { get; private set; }
    [JsonProperty("apiToken")] public string ApiToken { get; private set; }
    [JsonProperty("apiEndpoint")] public string ApiEndpoint { get; private set; }
}
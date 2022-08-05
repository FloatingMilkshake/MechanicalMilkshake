namespace MechanicalMilkshake.Modules;

public class WorkerLinks
{
    [JsonProperty("baseUrl")] public string BaseUrl { get; set; }

    [JsonProperty("secret")] public string Secret { get; set; }

    [JsonProperty("namespaceId")] public string NamespaceId { get; set; }

    [JsonProperty("apiKey")] public string ApiKey { get; set; }

    [JsonProperty("accountId")] public string AccountId { get; set; }

    [JsonProperty("email")] public string Email { get; set; }
}

public class S3
{
    [JsonProperty("bucket")] public string Bucket { get; set; }

    [JsonProperty("cdnBaseUrl")] public string CdnBaseUrl { get; set; }

    [JsonProperty("endpoint")] public string Endpoint { get; set; }

    [JsonProperty("accessKey")] public string AccessKey { get; set; }

    [JsonProperty("secretKey")] public string SecretKey { get; set; }

    [JsonProperty("region")] public string Region { get; set; }
}

public class Cloudflare
{
    [JsonProperty("urlPrefix")] public string UrlPrefix { get; set; }

    [JsonProperty("zoneId")] public string ZoneId { get; set; }

    [JsonProperty("token")] public string Token { get; set; }
}

public struct ConfigJson
{
    [JsonProperty("botToken")] public string BotToken { get; private set; }

    [JsonProperty("homeChannel")] public string HomeChannel { get; private set; }

    [JsonProperty("devServerId")] public ulong DevServerId { get; private set; }

    [JsonProperty("wolframAlphaAppId")] public string WolframAlphaAppId { get; private set; }

    [JsonProperty("workerLinks")] public WorkerLinks WorkerLinks { get; private set; }

    [JsonProperty("s3")] public S3 S3 { get; private set; }

    [JsonProperty("cloudflare")] public Cloudflare Cloudflare { get; private set; }

    [JsonProperty("authorizedUsers")] public string[] AuthorizedUsers { get; private set; }

    [JsonProperty("sshHosts")] public string[] SshHosts { get; private set; }
}
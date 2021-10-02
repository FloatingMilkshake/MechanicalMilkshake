using Newtonsoft.Json;

namespace DiscordBot.Configuration
{
    public struct ConfigJson
    {
        [JsonProperty("bot_token")]
        public string Token { get; private set; }

        [JsonProperty("home_channel")]
        public string HomeChannel { get; private set; }

        // This could be a uint or even a ulong - not sure what the app id is, so don't know the type.
        [JsonProperty("wolframalpha_app_id")]
        public string WolframAlphaAppId { get; private set; }

        [JsonProperty("worker_links_base_url")]
        public string WorkerLinksBaseUrl { get; private set; }

        [JsonProperty("worker_links_secret")]
        public string WorkerLinksSecret { get; private set; }

        [JsonProperty("s3_bucket")]
        public string S3_Bucket { get; private set; }

        [JsonProperty("cdn_base_url")]
        public string CDNBaseUrl { get; private set; }

        [JsonProperty("s3_endpoint")]
        public string S3_Endpoint { get; private set; }

        [JsonProperty("s3_access_key")]
        public string S3_Access_Key { get; private set; }

        [JsonProperty("s3_secret_key")]
        public string S3_Secret_Key { get; private set; }

        [JsonProperty("s3_region")]
        public string S3_Region { get; private set;  }

        [JsonProperty("cloudflare_url_prefix")]
        public string CloudFlare_Url_Prefix { get; private set; }

        [JsonProperty("cloudflare_zone_id")]
        public string CloudFlare_Zone_Id { get; private set; }

        [JsonProperty("cloudflare_token")]
        public string CloudFlare_Token { get; private set; }
    }
}

using Newtonsoft.Json;

namespace DiscordBot.Configuration
{
    public struct ConfigJson
    {
        [JsonProperty("bot_token")]
        public string Token { get; private set; }
    }
}

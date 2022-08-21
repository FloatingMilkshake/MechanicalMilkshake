namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

public class LinkCommands : ApplicationCommandModule
{
    [SlashCommandGroup("link", "Set, update, or delete a short link with Cloudflare worker-links.")]
    public class Link
    {
        [SlashCommand("set", "Set or update a short link with Cloudflare worker-links.")]
        public async Task SetLink(InteractionContext ctx,
            [Option("key", "Set a custom key for the short link.")]
            string key,
            [Option("url", "The URL the short link should point to.")]
            string url)
        {
            if (url.Contains('<')) url = url.Replace("<", "");

            if (url.Contains('>')) url = url.Replace(">", "");

            string baseUrl;
            if (Program.configjson.WorkerLinks.BaseUrl == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
                return;
            }

            baseUrl = Program.configjson.WorkerLinks.BaseUrl;

            using HttpClient httpClient = new()
            {
                BaseAddress = new Uri(baseUrl)
            };

            HttpRequestMessage request = null;

            if (key == "null" || key == "random" || key == "rand")
                request = new HttpRequestMessage(HttpMethod.Post, "");
            else
                request = new HttpRequestMessage(HttpMethod.Put, key);

            string secret;
            if (Program.configjson.WorkerLinks.Secret == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                return;
            }

            secret = Program.configjson.WorkerLinks.Secret;

            request.Headers.Add("Authorization", secret);
            request.Headers.Add("URL", url);

            var response = await httpClient.SendAsync(request);
            var httpStatusCode = (int)response.StatusCode;
            var httpStatus = response.StatusCode.ToString();
            var responseText = await response.Content.ReadAsStringAsync();
            if (responseText.Length > 1940)
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service."));

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
        }

        [SlashCommand("delete", "Delete a short link with Cloudflare worker-links.")]
        public async Task DeleteWorkerLink(InteractionContext ctx,
            [Option("link", "The key or URL of the short link to delete.")]
            string url)
        {
            string baseUrl;
            if (Program.configjson.WorkerLinks.BaseUrl == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
                return;
            }

            baseUrl = Program.configjson.WorkerLinks.BaseUrl;

            using HttpClient httpClient = new()
            {
                BaseAddress = new Uri(baseUrl)
            };

            if (!url.Contains(baseUrl)) url = $"{baseUrl}/{url}";

            string secret;
            if (Program.configjson.WorkerLinks.Secret == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                return;
            }

            secret = Program.configjson.WorkerLinks.Secret;

            HttpRequestMessage request = new(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", secret);

            var response = await httpClient.SendAsync(request);
            var httpStatusCode = (int)response.StatusCode;
            var httpStatus = response.StatusCode.ToString();
            var responseText = await response.Content.ReadAsStringAsync();
            if (responseText.Length > 1940)
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. Think about connecting this to a pastebin-like service."));

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
        }

        [SlashCommand("list", "List all short links configured with Cloudflare worker-links.")]
        public async Task ListWorkerLinks(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            if (Program.configjson.WorkerLinks.ApiKey == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "Error: missing Cloudflare API Key! Make sure the apiKey field under workerLinks in your config.json file is set.")
                    .AsEphemeral());
                return;
            }

            if (Program.configjson.WorkerLinks.NamespaceId == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "Error: missing KV Namespace ID! Make sure the namespaceId field under workerLinks in your config.json file is set.")
                    .AsEphemeral());
                return;
            }

            if (Program.configjson.WorkerLinks.AccountId == null)
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "Error: missing Cloudflare Account ID! Make sure the accountId field under workerLinks in your config.json file is set.")
                    .AsEphemeral());

            if (Program.configjson.WorkerLinks.Email == null)
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "Error: missing email address for Cloudflare! Make sure the email field under workerLinks in your config.json file is set.")
                    .AsEphemeral());

            var requestUri =
                $"https://api.cloudflare.com/client/v4/accounts/{Program.configjson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}/keys";
            HttpRequestMessage request = new(HttpMethod.Get, requestUri);

            request.Headers.Add("X-Auth-Key", Program.configjson.WorkerLinks.ApiKey);
            request.Headers.Add("X-Auth-Email", Program.configjson.WorkerLinks.Email);
            var response = await Program.httpClient.SendAsync(request);

            var responseText = await response.Content.ReadAsStringAsync();

            var parsedResponse = JsonConvert.DeserializeObject<CloudflareResponse>(responseText);

            var kvListResponse = "";

            foreach (var item in parsedResponse.Result)
            {
                var key = item.Name.Replace("/", "%2F");

                var valueRequestUri =
                    $"https://api.cloudflare.com/client/v4/accounts/{Program.configjson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}/values/{key}";
                HttpRequestMessage valueRequest = new(HttpMethod.Get, valueRequestUri);

                valueRequest.Headers.Add("X-Auth-Key", Program.configjson.WorkerLinks.ApiKey);
                valueRequest.Headers.Add("X-Auth-Email", Program.configjson.WorkerLinks.Email);
                var valueResponse = await Program.httpClient.SendAsync(valueRequest);

                var value = await valueResponse.Content.ReadAsStringAsync();
                value = value.Replace(value, $"<{value}>");
                kvListResponse += $"`{item.Name}`: {value}\n\n";
            }

            DiscordEmbedBuilder embed = new()
            {
                Title = "Link List"
            };
            try
            {
                embed.Description = kvListResponse;
            }
            catch (ArgumentException ex) when (ex.Message.Contains("length cannot exceed"))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"I couldn't send the list of links here! It's too long to fit into a message. You can see the full list on Cloudflare's website [here](https://dash.cloudflare.com/{Program.configjson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}).")
                    .AsEphemeral());
                return;
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"Hmm, I couldn't send the list of links here! You can see the full list on Cloudflare's website [here](https://dash.cloudflare.com/{Program.configjson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}). Exception details are below.\n```\n{ex.GetType()}: {ex.Message}\n```")
                    .AsEphemeral());
                return;
            }

            try
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"Hmm, I couldn't send the list of links here! You can see the full list on Cloudflare's website [here](https://dash.cloudflare.com/{Program.configjson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.configjson.WorkerLinks.NamespaceId}). Exception details are below.\n```\n{ex.GetType()}: {ex.Message}\n```")
                    .AsEphemeral());
            }
        }

        [SlashCommand("get", "Get the long URL for a short link.")]
        public async Task LinkShow(InteractionContext ctx,
            [Option("link", "The key or URL of the short link to get.")]
            string url)
        {
            string baseUrl;
            if (Program.configjson.WorkerLinks.BaseUrl == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
                return;
            }

            string secret;
            if (Program.configjson.WorkerLinks.Secret == null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                return;
            }

            secret = Program.configjson.WorkerLinks.Secret;

            baseUrl = Program.configjson.WorkerLinks.BaseUrl;

            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };

            using HttpClient httpClient = new(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };

            if (!url.Contains(baseUrl)) url = $"{baseUrl}/{url}";

            HttpRequestMessage request = new(HttpMethod.Get, url);
            request.Headers.Add("Authorization", secret);

            var response = await httpClient.SendAsync(request);
            var httpStatusCode = (int)response.StatusCode;
            var httpStatus = response.StatusCode.ToString();

            if (response.Headers.Location is null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        "That link doesn't exist!"));
                return;
            }

            await ctx.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    $"<{url}>\npoints to:\n<{response.Headers.Location}>"));
        }

        public class CloudflareResponse
        {
            [JsonProperty("result")] public List<KVEntry> Result { get; set; }
        }

        public class KVEntry
        {
            [JsonProperty("name")] public string Name { get; set; }
        }
    }
}
namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

public class LinkCommands : ApplicationCommandModule
{
    [SlashCommandGroup("link", "Set, update, or delete a short link with Cloudflare worker-links.")]
    public class Link
    {
        [SlashCommand("set", "Set or update a short link with Cloudflare worker-links.")]
        public static async Task SetLink(InteractionContext ctx,
            [Option("key", "Set a custom key for the short link.")]
            string key,
            [Option("url", "The URL the short link should point to.")]
            string url)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder());
        
            if (Program.DisabledCommands.Contains("wl"))
            {
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
                return;
            }

            if (url.Contains('<')) url = url.Replace("<", "");

            if (url.Contains('>')) url = url.Replace(">", "");

            if (Program.ConfigJson.WorkerLinks.BaseUrl is null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
                return;
            }

            var baseUrl = Program.ConfigJson.WorkerLinks.BaseUrl;

            using HttpClient httpClient = new()
            {
                BaseAddress = new Uri(baseUrl)
            };

            var request = key is "null" or "random" or "rand"
                ? new HttpRequestMessage(HttpMethod.Post, "")
                : new HttpRequestMessage(HttpMethod.Put, key);

            if (Program.ConfigJson.WorkerLinks.Secret is null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                return;
            }

            var secret = Program.ConfigJson.WorkerLinks.Secret;

            request.Headers.Add("Authorization", secret);
            request.Headers.Add("URL", url);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"An exception occurred while trying to send the request! `{ex.GetType()}: {ex.Message}`"));
                return;
            }
            
            var httpStatusCode = (int)response.StatusCode;
            var httpStatus = response.StatusCode.ToString();
            var responseText = await response.Content.ReadAsStringAsync();
            if (responseText.Length > 1947)
            {
                var hasteUrl = await HastebinHelpers.UploadToHastebinAsync(responseText);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. It was uploaded to Hastebin here: {hasteUrl}"));
                return;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
        }

        [SlashCommand("delete", "Delete a short link with Cloudflare worker-links.")]
        public static async Task DeleteWorkerLink(InteractionContext ctx,
            [Option("link", "The key or URL of the short link to delete.")]
            string url)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            
            if (Program.DisabledCommands.Contains("wl"))
            {
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
                return;
            }

            var baseUrl = Program.ConfigJson.WorkerLinks.BaseUrl;

            using HttpClient httpClient = new()
            {
                BaseAddress = new Uri(baseUrl)
            };

            if (!url.Contains(baseUrl)) url = $"{baseUrl}/{url}";

            if (Program.ConfigJson.WorkerLinks.Secret is null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                return;
            }

            var secret = Program.ConfigJson.WorkerLinks.Secret;

            HttpRequestMessage request = new(HttpMethod.Delete, url);
            request.Headers.Add("Authorization", secret);

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"An exception occurred while trying to send the request! `{ex.GetType()}: {ex.Message}`"));
                return;
            }
            
            var httpStatusCode = (int)response.StatusCode;
            var httpStatus = response.StatusCode.ToString();
            var responseText = await response.Content.ReadAsStringAsync();
            if (responseText.Length > 1947)
            {
                var hasteUrl = await HastebinHelpers.UploadToHastebinAsync(responseText);
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. It was uploaded to Hastebin here: {hasteUrl}"));
                return;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
        }

        [SlashCommand("list", "List all short links configured with Cloudflare worker-links.")]
        public static async Task ListWorkerLinks(InteractionContext ctx)
        {
            if (Program.DisabledCommands.Contains("wl"))
            {
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, false);
                return;
            }

            await ctx.CreateResponseAsync(
                $"You can view the list of short links at Cloudflare [here](https://dash.cloudflare.com/{Program.ConfigJson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.ConfigJson.WorkerLinks.NamespaceId})!");
        }

        [SlashCommand("get", "Get the long URL for a short link.")]
        public static async Task LinkShow(InteractionContext ctx,
            [Option("link", "The key or URL of the short link to get.")]
            string url)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            
            if (Program.DisabledCommands.Contains("wl"))
            {
                await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
                return;
            }

            if (Program.ConfigJson.WorkerLinks.Secret is null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Error: No secret provided! Make sure the secret field under workerLinks in your config.json file is set."));
                return;
            }

            var secret = Program.ConfigJson.WorkerLinks.Secret;

            var baseUrl = Program.ConfigJson.WorkerLinks.BaseUrl;

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

            HttpResponseMessage response;
            try
            {
                response = await httpClient.SendAsync(request);
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"An exception occurred while trying to send the request! `{ex.GetType()}: {ex.Message}`"));
                return;
            }

            if (response.Headers.Location is null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "That link doesn't exist!"));
                return;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"<{url}>\npoints to:\n<{response.Headers.Location}>"));
        }

        public class CloudflareResponse
        {
            [JsonProperty("result")] public List<KvEntry> Result { get; set; }
        }

        public class KvEntry
        {
            [JsonProperty("name")] public string Name { get; set; }
        }
    }
}
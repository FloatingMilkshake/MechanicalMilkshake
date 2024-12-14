namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands;

[Command("link")]
[Description("Set, update, or delete a short link.")]
public class LinkCommands
{
    [Command("set")]
    [Description("Set or update a short link.")]
    public static async Task SetWorkerLink(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("key"), Description("Set a custom key for the short link.")]
        string key,
        [Parameter("url"), Description("The URL the short link should point to.")]
        string url)
    {
        await ctx.DeferResponseAsync();
    
        if (Program.DisabledCommands.Contains("wl"))
        {
            await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
            return;
        }

        if (url.Contains('<')) url = url.Replace("<", "");

        if (url.Contains('>')) url = url.Replace(">", "");
        
        if (key[0] == '/') key = key[1..];

        if (Program.ConfigJson.WorkerLinks.BaseUrl is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "Error: No base URL provided! Make sure the baseUrl field under workerLinks in your config.json file is set."));
            return;
        }

        var baseUrl = Program.ConfigJson.WorkerLinks.BaseUrl;

        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(baseUrl);

        var request = key is "null" or "random" or "rand"
            ? new HttpRequestMessage(HttpMethod.Post, "")
            : new HttpRequestMessage(HttpMethod.Put, key);

        if (Program.ConfigJson.WorkerLinks.Secret is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
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
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"An exception occurred while trying to send the request! `{ex.GetType()}: {ex.Message}`"));
            return;
        }
        
        var httpStatusCode = (int)response.StatusCode;
        var httpStatus = response.StatusCode.ToString();
        var responseText = await response.Content.ReadAsStringAsync();
        if (responseText.Length > 1947)
        {
            var hasteUrl = await HastebinHelpers.UploadToHastebinAsync(responseText);
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. It was uploaded to Hastebin here: {hasteUrl}"));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
            $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
    }

    [Command("delete")]
    [Description("Delete a short link.")]
    public static async Task DeleteWorkerLink(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("link"), Description("The key or URL of the short link to delete.")]
        string url)
    {
        await ctx.DeferResponseAsync();
        
        if (Program.DisabledCommands.Contains("wl"))
        {
            await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
            return;
        }
        
        if (url[0] == '/') url = url[1..];

        var baseUrl = Program.ConfigJson.WorkerLinks.BaseUrl;

        using HttpClient httpClient = new();
        httpClient.BaseAddress = new Uri(baseUrl);

        if (!url.Contains(baseUrl)) url = $"{baseUrl}/{url}";

        if (Program.ConfigJson.WorkerLinks.Secret is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
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
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"An exception occurred while trying to send the request! `{ex.GetType()}: {ex.Message}`"));
            return;
        }
        
        var httpStatusCode = (int)response.StatusCode;
        var httpStatus = response.StatusCode.ToString();
        var responseText = await response.Content.ReadAsStringAsync();
        if (responseText.Length > 1947)
        {
            var hasteUrl = await HastebinHelpers.UploadToHastebinAsync(responseText);
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Worker responded with code: `{httpStatusCode}`...but the full response is too long to post here. It was uploaded to Hastebin here: {hasteUrl}"));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
            $"Worker responded with code: `{httpStatusCode}` (`{httpStatus}`)\n```json\n{responseText}\n```"));
    }

    [Command("list")]
    [Description("List all short links.")]
    public static async Task ListWorkerLinks(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("match_keys"), Description("Optionally filter by key.")] string keyFilter = "",
        [Parameter("match_values"), Description("Optionally filter by value.")] string valueFilter = "",
        [Parameter("exact_match"), Description("Whether to match filters exactly. Defaults to false.")] bool exactMatch = false)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Working on it..."));
        
        if (Program.DisabledCommands.Contains("wl"))
        {
            await CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
            return;
        }

        var requestUri =
            $"https://api.cloudflare.com/client/v4/accounts/{Program.ConfigJson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.ConfigJson.WorkerLinks.NamespaceId}/keys";
        HttpRequestMessage request = new(HttpMethod.Get, requestUri);

        request.Headers.Add("X-Auth-Key", Program.ConfigJson.WorkerLinks.ApiKey);
        request.Headers.Add("X-Auth-Email", Program.ConfigJson.WorkerLinks.Email);
        var response = await Program.HttpClient.SendAsync(request);

        var responseText = await response.Content.ReadAsStringAsync();

        var parsedResponse = JsonConvert.DeserializeObject<CloudflareResponse>(responseText);

        var kvListResponse = "";

        foreach (var item in parsedResponse.Result)
        {
            var key = item.Name.Replace("/", "%2F");
            
            // Check key filter; if key does not match, skip
            if (exactMatch)
            {
                if (!string.IsNullOrWhiteSpace(keyFilter) && key != keyFilter.Replace("/", "%2F")) continue;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(keyFilter) && !key.Contains(keyFilter.Replace("/", "%2F"))) continue;
            }

            var valueRequestUri =
                $"https://api.cloudflare.com/client/v4/accounts/{Program.ConfigJson.WorkerLinks.AccountId}/storage/kv/namespaces/{Program.ConfigJson.WorkerLinks.NamespaceId}/values/{key}";
            HttpRequestMessage valueRequest = new(HttpMethod.Get, valueRequestUri);

            valueRequest.Headers.Add("X-Auth-Key", Program.ConfigJson.WorkerLinks.ApiKey);
            valueRequest.Headers.Add("X-Auth-Email", Program.ConfigJson.WorkerLinks.Email);
            var valueResponse = await Program.HttpClient.SendAsync(valueRequest);

            var value = await valueResponse.Content.ReadAsStringAsync();
            value = value.Replace(value, $"<{value}>");
            
            // Check value filter; if value does not match, skip
            if (exactMatch)
            {
                if (!string.IsNullOrWhiteSpace(valueFilter) &&
                    value.Replace("<", "").Replace(">", "") != valueFilter) continue;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(valueFilter) && !value.Contains(valueFilter)) continue;
            }
            
            kvListResponse += $"**{item.Name}**: {value}\n\n";
        }

        DiscordEmbedBuilder embed = new()
        {
            Title = string.IsNullOrWhiteSpace(keyFilter) && string.IsNullOrWhiteSpace(valueFilter)
                ? "Link List"
                : "Matching Links",
            Color = Program.BotColor
        };

        if (string.IsNullOrWhiteSpace(kvListResponse))
        {
            embed.Description = "No links matched the specified filters.";
            embed.Color = DiscordColor.Red;
            
            await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed));
            return;
        }
        
        try
        {
            var pages = InteractivityExtension.GeneratePagesInEmbed(kvListResponse, SplitType.Line, embed).ToList();

            var leftSkip = new DiscordButtonComponent(DiscordButtonStyle.Primary, "leftskip", "<<<");
            var left = new DiscordButtonComponent(DiscordButtonStyle.Primary, "left", "<");
            var right = new DiscordButtonComponent(DiscordButtonStyle.Primary, "right", ">");
            var rightSkip = new DiscordButtonComponent(DiscordButtonStyle.Primary, "rightskip", ">>>");
            var stop = new DiscordButtonComponent(DiscordButtonStyle.Danger, "stop", "Stop");

            if (pages.Count > 1)
                await ctx.Interaction.SendPaginatedResponseAsync(false, ctx.User, pages,
                    new PaginationButtons
                        { SkipLeft = leftSkip, Left = left, Right = right, SkipRight = rightSkip, Stop = stop },
                    deletion: ButtonPaginationBehavior.DeleteMessage,
                    asEditResponse: true);
            else
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().AddEmbed(embed.WithDescription(kvListResponse)));
        }
        catch (Exception ex)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Hmm, I couldn't send the list of links here!" +
                $" You can see the full list on Cloudflare's website [here](https://dash.cloudflare.com/" +
                $"{Program.ConfigJson.WorkerLinks.AccountId}/workers/kv/namespaces/{Program.ConfigJson.WorkerLinks.NamespaceId})." +
                $" Exception details are below.\n```\n{ex.GetType()}: {ex.Message}\n```"));
        }
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
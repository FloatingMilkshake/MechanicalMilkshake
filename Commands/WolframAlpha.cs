namespace MechanicalMilkshake.Commands;

public class WolframAlpha
{
    private const string QueryUrlLinkDisplayText = "Click here for more details";

    [Command("wolframalpha")]
    [Description("Search WolframAlpha without leaving Discord!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task WolframAlphaCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("query"), Description("What to search for.")]
        string query)
    {
        await ctx.DeferResponseAsync();

        if (Program.DisabledCommands.Contains("wa"))
        {
            await CommandHelpers.FailOnMissingInfo(ctx, true);
            return;
        }

        if (query is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "Hmm, it doesn't look like you entered a valid query. Try something like `/wolframalpha query:What is the meaning of life?`."));
            return;
        }

        var queryEncoded = HttpUtility.UrlEncode(query).Replace("(", "%28").Replace(")", "%29");

        var appid = Program.ConfigJson.WolframAlphaAppId;

        var queryEscaped = query.Replace("`", @"\`")
            .Replace("*", @"\*")
            .Replace("_", @"\_")
            .Replace("~", @"\~")
            .Replace(">", @"\>");
        
        var response = new DiscordMessageBuilder();

        // Text response
        string textResponse = default;
        try
        {
            textResponse = await Program.HttpClient.GetStringAsync($"https://api.wolframalpha.com/v1/result?appid={appid}&i={queryEncoded}");
        }
        catch
        {
            // kaboom
            // (don't need to do anything here)
        }
            
        // Image response
        MemoryStream imageResponse = default;
        try
        {
            var data = await Program.HttpClient.GetByteArrayAsync($"https://api.wolframalpha.com/v1/simple?appid={appid}&i={queryEncoded}");
            imageResponse = new MemoryStream(data);
        }
        catch
        {
            // kaboom
            // (don't need to do anything here)
        }
        
        if (textResponse == default && imageResponse == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "Hmm, WolframAlpha didn't have an answer to that query! Try rephrasing it, or check your spelling."));
            return;
        }
        
        response.Content += $"> {queryEscaped}";
        
        if (textResponse != default)
            response.Content += $"\n{(imageResponse != default ? "**Simple answer:** " : "")}{textResponse}";
        
        if (imageResponse != default)
        {
            if (textResponse != default) response.Content += "\n**Extended answer:**";
            response.AddFile("result.gif", imageResponse);
        }
        
        await ctx.FollowupAsync(response);
    }
}
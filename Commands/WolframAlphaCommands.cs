namespace MechanicalMilkshake.Commands;

internal class WolframAlphaCommands
{
    [Command("wolframalpha")]
    [Description("Search WolframAlpha without leaving Discord!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    public static async Task WolframAlphaCommandAsync(SlashCommandContext ctx,
        [Parameter("query"), Description("What to search for.")]
        string query)
    {
        await ctx.DeferResponseAsync();

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.WolframAlphaAppId))
        {
            await ctx.FollowupAsync("Sorry, this command is unavailable! Please contact a bot owner for help.");
            return;
        }

        var queryEncoded = HttpUtility.UrlEncode(query).Replace("(", "%28").Replace(")", "%29");

        var appid = Setup.Configuration.ConfigJson.WolframAlphaAppId;

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
            textResponse = await Setup.Constants.HttpClient.GetStringAsync($"https://api.wolframalpha.com/v1/result?appid={appid}&i={queryEncoded}");
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
            var data = await Setup.Constants.HttpClient.GetByteArrayAsync($"https://api.wolframalpha.com/v1/simple?appid={appid}&i={queryEncoded}");
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
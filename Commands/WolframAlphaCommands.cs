namespace MechanicalMilkshake.Commands;

internal class WolframAlphaCommands
{
    [Command("wolframalpha")]
    [Description("Search WolframAlpha without leaving Discord!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task WolframAlphaCommandAsync(SlashCommandContext ctx,
        [Parameter("query"), Description("What to search for.")]
        string query)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(false));

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.WolframAlphaAppId))
        {
            await ctx.FollowupAsync("Sorry, this command is unavailable! Please contact a bot owner for help.", ephemeral: ctx.ShouldUseEphemeralResponse(false));
            return;
        }

        var queryEncoded = HttpUtility.UrlEncode(query).Replace("(", "%28").Replace(")", "%29");

        var appid = Setup.Configuration.ConfigJson.WolframAlphaAppId;

        var queryEscaped = query.Replace("`", @"\`")
            .Replace("*", @"\*")
            .Replace("_", @"\_")
            .Replace("~", @"\~")
            .Replace(">", @"\>");

        var response = new DiscordFollowupMessageBuilder();

        // Text response
        string textResponse = default;
        try
        {
            textResponse = await Setup.Constants.HttpClient.GetStringAsync($"https://api.wolframalpha.com/v1/result?appid={appid}&i={queryEncoded}");
        }
        catch (HttpRequestException)
        {
            // WolframAlpha doesn't have a response for this query type or an error occurred
            // Errors are handled later
        }

        // Image response
        MemoryStream imageResponse = default;
        try
        {
            imageResponse = new MemoryStream(await Setup.Constants.HttpClient.GetByteArrayAsync($"https://api.wolframalpha.com/v1/simple?appid={appid}&i={queryEncoded}"));
        }
        catch (HttpRequestException)
        {
            // WolframAlpha doesn't have a response for this query type or an error occurred
            // Errors are handled later
        }

        if (textResponse == default && imageResponse == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Hmm, WolframAlpha didn't have an answer to that query! Try rephrasing it.")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
            return;
        }

        response.Content += $"> {queryEscaped}";

        if (textResponse != default)
            response.Content += $"\n{(imageResponse != default ? "**Simple answer:** " : "")}{textResponse}";

        if (imageResponse != default)
        {
            if (textResponse != default)
                response.Content += "\n**Extended answer:**";
            response.AddFile("result.gif", imageResponse);
        }

        await ctx.FollowupAsync(response.AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
    }
}

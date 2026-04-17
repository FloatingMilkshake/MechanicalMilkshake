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

        string text = default;
        var textApiResponse = await Setup.Constants.HttpClient.GetAsync($"https://api.wolframalpha.com/v1/result?appid={appid}&i={queryEncoded}");
        if (textApiResponse.IsSuccessStatusCode)
            text = await textApiResponse.Content.ReadAsStringAsync();

        MemoryStream image = default;
        var imageApiResponse = await Setup.Constants.HttpClient.GetAsync($"https://api.wolframalpha.com/v1/simple?appid={appid}&i={queryEncoded}");
        if (imageApiResponse.IsSuccessStatusCode)
            image = new MemoryStream(await imageApiResponse.Content.ReadAsByteArrayAsync());
        

        if (text == default && image == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Hmm, WolframAlpha didn't have an answer to that query! Try rephrasing it.")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
            return;
        }

        response.Content += $"> {queryEscaped}";

        if (text != default)
            response.Content += $"\n{(image != default ? "**Simple answer:** " : "")}{text}";

        if (image != default)
        {
            if (text != default)
                response.Content += "\n**Extended answer:**";
            response.AddFile("result.gif", image);
        }

        await ctx.FollowupAsync(response.AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
    }
}

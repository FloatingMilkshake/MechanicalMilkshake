namespace MechanicalMilkshake.Commands;

[Command("wolframalpha")]
[Description("Search WolframAlpha without leaving Discord!")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
[InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
internal class WolframAlphaCommands
{
    [Command("simple")]
    [Description("Get a simple text-only response from WolframAlpha. Fastest, supports many queries.")]
    public static async Task WolframAlphaSimpleCommandAsync(SlashCommandContext ctx,
        [Parameter("query"), Description("What do you want to know?")] string query)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        var (success, result, _) = await SendWolframAlphaQueryAsync(query, WolframAlphaQueryType.Text);
        if (success)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(result)
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
        else
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("It looks like WolframAlpha didn't have a simple answer for that query!")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
    }

    [Command("detailed")]
    [Description("Get a more-detailed image response from WolframAlpha. A bit slower, but supports almost all queries.")]
    public static async Task WolframAlphaDetailedCommandAsync(SlashCommandContext ctx,
        [Parameter("query"), Description("Wht do you want to know?")] string query)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        var (success, _, result) = await SendWolframAlphaQueryAsync(query, WolframAlphaQueryType.Image);
        if (success)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .AddFile("result.gif", result)
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
        else
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("It looks like WolframAlpha didn't have a detailed answer for that query!")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
    }

    private static async Task<(bool success, string textResult, MemoryStream imageResult)> SendWolframAlphaQueryAsync(string query, WolframAlphaQueryType queryType)
    {
        var queryEncoded = HttpUtility.UrlEncode(query)
            .Replace("(", "%28")
            .Replace(")", "%29");

        var queryEscaped = query.Replace("`", @"\`")
            .Replace("*", @"\*")
            .Replace("_", @"\_")
            .Replace("~", @"\~")
            .Replace(">", @"\>");

        if (queryType == WolframAlphaQueryType.Text)
        {
            var textApiResponse = await Setup.Constants.HttpClient
                .GetAsync($"https://api.wolframalpha.com/v1/result?appid={Setup.State.Process.Configuration.WolframAlphaAppId}&i={queryEncoded}");
            if (textApiResponse.IsSuccessStatusCode)
                return (true, await textApiResponse.Content.ReadAsStringAsync(), null);
            else
                return (false, null, null);
        }
        else if (queryType == WolframAlphaQueryType.Image)
        {
            var imageApiResponse = await Setup.Constants.HttpClient
                .GetAsync($"https://api.wolframalpha.com/v1/simple?appid={Setup.State.Process.Configuration.WolframAlphaAppId}&i={queryEncoded}");
            if (imageApiResponse.IsSuccessStatusCode)
                return (true, null, new MemoryStream(await imageApiResponse.Content.ReadAsByteArrayAsync()));
            else
                return (false, null, null);
        }
        else
        {
            throw new ArgumentException("Invalid WolframAlpha query type");
        }
    }

    private enum WolframAlphaQueryType
    {
        Text = 0,
        Image = 1
    }
}

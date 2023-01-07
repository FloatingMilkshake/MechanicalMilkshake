namespace MechanicalMilkshake.Commands;

public class WolframAlpha : ApplicationCommandModule
{
    [SlashCommand("wolframalpha", "Search WolframAlpha without leaving Discord!")]
    public static async Task WolframAlphaCommand(InteractionContext ctx,
        [Option("query", "What to search for.")]
        string query,
        [Option("response_type",
            "Whether the response should be simple text only or a more-detailed image. Defaults to Text.")]
        [Choice("Text", "text")]
        [Choice("Image", "image")]
        string responseType = "text")
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (Program.DisabledCommands.Contains("cdn"))
        {
            CommandHandlerHelpers.FailOnMissingInfo(ctx, true);
            return;
        }

        if (query is null)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                "Hmm, it doesn't look like you entered a valid query. Try something like `/wolframalpha query:What is the meaning of life?`."));
            return;
        }

        var queryEncoded = HttpUtility.UrlEncode(query).Replace("(", "%28").Replace(")", "%29");

        var appid = Program.ConfigJson.Base.WolframAlphaAppId;

        var queryEscaped = query.Replace("`", @"\`")
            .Replace("*", @"\*")
            .Replace("_", @"\_")
            .Replace("~", @"\~")
            .Replace(">", @"\>");

        if (responseType == "text")
            try
            {
                var data = await Program.HttpClient.GetStringAsync(
                    $"https://api.wolframalpha.com/v1/result?appid={appid}&i={queryEncoded}");

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"> {queryEscaped}\n" +
                    data + $"\n\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)"));
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Something went wrong while searching WolframAlpha and I couldn't get a simple answer for your query! You might have better luck if you set `responsetype` to `Image`.\n\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)"));
            }
        else
            try
            {
                var data =
                    await Program.HttpClient.GetByteArrayAsync(
                        $"https://api.wolframalpha.com/v1/simple?appid={appid}&i={queryEncoded}");
                var stream = new MemoryStream(data);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        $"> {queryEscaped}\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)")
                    .AddFile("result.gif", stream));
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Something went wrong while searching WolframAlpha and I couldn't get an image response for your query! You might have better luck if you set `responsetype` to `Text`.\n\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)"));
            }
    }
}
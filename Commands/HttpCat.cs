namespace MechanicalMilkshake.Commands;

public class HttpCat : ApplicationCommandModule
{
    private static readonly List<int> HttpCodes = new()
    {
        100, 101, 102, 200, 201, 202, 203, 204, 206, 207, 300, 301, 302, 303, 304, 305, 307, 308, 400, 401, 402,
        403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 420, 421, 422, 423, 424,
        425, 426, 429, 431, 444, 450, 451, 497, 498, 499, 500, 501, 502, 503, 504, 506, 507, 508, 509, 510, 511,
        521, 522, 523, 525, 599
    };

    [SlashCommand("httpcat", "Get an HTTP cat image!")]
    public static async Task HttpCatCommand(InteractionContext ctx,
        [Option("code", "The code to get the http.cat image for.")]
        long? code = null)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (code is null)
        {
            var random = new Random();
            var randomCode = HttpCodes[random.Next(HttpCodes.Count)];

            var httpResponse =
                await Program.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                    $"https://http.cat/{randomCode}"));
            if (httpResponse.IsSuccessStatusCode)
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent($"https://http.cat/{randomCode}"));
            else
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I ran into an error trying to get a random HTTP cat image!" +
                    $"Request returned code `{(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}` for HTTP code {randomCode}."));
        }
        else
        {
            if (!HttpCodes.Contains((int)code))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Either that's not a valid HTTP status code, or it's not supported by http.cat! Try another one."));
                return;
            }

            var httpResponse =
                await Program.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://http.cat/{code}"));
            if (httpResponse.IsSuccessStatusCode)
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent($"https://http.cat/{code}"));
            else
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I ran into an error trying to get a random HTTP cat image!" +
                    $"Request returned code `{(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}` for HTTP code {code}."));
        }
    }
}
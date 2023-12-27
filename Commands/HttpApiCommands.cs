namespace MechanicalMilkshake.Commands;

public class HttpApiCommands : ApplicationCommandModule
{
    private static readonly List<int> HttpCodes =
    [
        100, 101, 102, 200, 201, 202, 203, 204, 206, 207, 300, 301, 302, 303, 304, 305, 307, 308, 400, 401, 402,
        403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418, 420, 421, 422, 423, 424,
        425, 426, 429, 431, 444, 450, 451, 497, 498, 499, 500, 501, 502, 503, 504, 506, 507, 508, 509, 510, 511,
        521, 522, 523, 525, 599
    ];

    [SlashCommand("httpcat", "Get an http.cat image!")]
    public static async Task HttpCatCommand(InteractionContext ctx,
        [Option("code", "The code to get the http.cat image for.")] [Minimum(100)] [Maximum(599)]
        long? code = null)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(GetHttpImage(code, true)));
    }

    [SlashCommand("httpdog", "Get an http.dog image!")]
    public static async Task HttpDogCommand(InteractionContext ctx,
        [Option("code", "The code to get the http.dog image for.")] [Minimum(100)] [Maximum(599)]
        long? code = null)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(GetHttpImage(code, false)));
    }

    private static string GetHttpImage(long? code, bool isCat)
    {
        var tld = isCat ? "cat" : "dog";

        if (code is null)
        {
            var randomCode = GetRandomHttpCode();

            var httpResponse =
                Program.HttpClient.Send(new HttpRequestMessage(HttpMethod.Get,
                    $"https://http.{tld}/{randomCode}.jpg"));
            if (httpResponse.IsSuccessStatusCode)
                return $"https://http.{tld}/{randomCode}.jpg";

            return $"I ran into an error trying to get a random http.{tld} image!" +
                   $"Request returned code `{(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}` for HTTP code {randomCode}.";
        }
        else
        {
            if (!HttpCodes.Contains((int)code))
                return
                    $"Either that's not a valid HTTP status code, or it's not supported by http.{tld}! Try another one.";

            var httpResponse =
                Program.HttpClient.Send(new HttpRequestMessage(HttpMethod.Get, $"https://http.{tld}/{code}.jpg"));
            if (httpResponse.IsSuccessStatusCode)
                return $"https://http.{tld}/{code}.jpg";

            return "I ran into an error trying to get a random HTTP cat image!" +
                   $"Request returned code `{(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}` for HTTP code {code}.";
        }
    }

    private static int GetRandomHttpCode()
    {
        var random = new Random();
        return HttpCodes[random.Next(HttpCodes.Count)];
    }
}
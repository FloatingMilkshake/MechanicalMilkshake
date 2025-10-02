namespace MechanicalMilkshake.Commands;

public class HttpApiCommands
{
    private static readonly List<int> HttpCodes =
    [
        100, 101, 102, 103, 200, 201, 202, 203, 204, 205, 206, 207, 208, 214, 226, 300, 301, 302, 303, 304, 305,
        307, 308, 400, 401, 402, 403, 404, 405, 406, 407, 408, 409, 410, 411, 412, 413, 414, 415, 416, 417, 418,
        419, 420, 421, 422, 423, 424, 425, 426, 428, 429, 431, 444, 450, 451, 495, 496, 497, 498, 499, 500, 501,
        502, 503, 504, 506, 507, 508, 509, 510, 511, 521, 522, 523, 525, 530, 599
    ];

    [Command("httpcat")]
    [Description("Get an http.cat image!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task HttpCatCommand(SlashCommandContext ctx,
        [Parameter("code"), Description("The code to get the http.cat image for.")] [MinMaxValue(100, 599)]
        int? code = null)
    {
        await ctx.DeferResponseAsync();

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(GetHttpImage(code, true)));
    }

    [Command("httpdog")]
    [Description("Get an http.dog image!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task HttpDogCommand(SlashCommandContext ctx,
        [Parameter("code"), Description("The code to get the http.dog image for.")] [MinMaxValue(100, 599)]
        int? code = null)
    {
        await ctx.DeferResponseAsync();

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(GetHttpImage(code, false)));
    }

    private static string GetHttpImage(int? code, bool isCat)
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
            if (!HttpCodes.Contains(code.Value))
                return
                    $"Either that's not a valid HTTP status code, or it's not supported! Try another one.";

            var httpResponse =
                Program.HttpClient.Send(new HttpRequestMessage(HttpMethod.Get, $"https://http.{tld}/{code}.jpg"));
            if (httpResponse.IsSuccessStatusCode)
                return $"https://http.{tld}/{code}.jpg";

            return $"I ran into an error trying to get a random HTTP {tld} image!" +
                   $"Request returned code `{(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}` for HTTP code {code}.";
        }
    }

    private static int GetRandomHttpCode()
    {
        var random = new Random();
        return HttpCodes[random.Next(HttpCodes.Count)];
    }
}
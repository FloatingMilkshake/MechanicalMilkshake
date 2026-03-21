namespace MechanicalMilkshake.Commands;

public class HttpCatCommands
{
    [Command("httpcat")]
    [Description("Get an http.cat image!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task HttpCatCommand(SlashCommandContext ctx,
        [Parameter("code"), Description("The code to get the http.cat image for.")] [MinMaxValue(100, 599)]
        int? code = null)
    {
        await ctx.DeferResponseAsync();

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(await GetHttpImageAsync(code, "cat")));
    }

    private static async Task<string> GetHttpImageAsync(int? code, string tld)
    {
        bool randomCode = false;
        var random = new Random();
        if (code is null)
        {
            code = random.Next(100, 600);
            randomCode = true;
        }

        var httpResponse = await Program.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://http.{tld}/{code}.jpg"));

        while (!httpResponse.IsSuccessStatusCode)
        {
            if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            {
                if (randomCode)
                {
                    code = random.Next(100, 600);
                    httpResponse = await Program.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://http.{tld}/{code}.jpg"));
                }
                else return "Either that's not a valid HTTP status code, or it's not supported! Try another one.";
            }
            else if (httpResponse.StatusCode == HttpStatusCode.TooManyRequests)
            {
                await Task.Delay(2000);
                httpResponse = await Program.HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get, $"https://http.{tld}/{code}.jpg"));
            }
            else return $"I ran into an error trying to get a random http.{tld} image!" +
                    $"Request returned code `{(int)httpResponse.StatusCode}: {httpResponse.ReasonPhrase}` for HTTP code {code}.";
        }

        return $"https://http.{tld}/{code}.jpg";
    }
}
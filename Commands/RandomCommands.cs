namespace MechanicalMilkshake.Commands;

[Command("random")]
[Description("Get a random number, fact, or picture of a dog or cat.")]
public partial class RandomCmds
{
    [Command("number")]
    [Description("Generates a random number between two that you specify.")]
    public static async Task RandomNumber(SlashCommandContext ctx,
        [Parameter("min"), Description("The minimum number to choose between. Defaults to 1.")]
        long min = 1,
        [Parameter("max"), Description("The maximum number to choose between. Defaults to 10.")]
        long max = 10)
    {
        if (min > max)
        {
            await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent(
                    "The minimum number cannot be greater than the maximum number!"));
            return;
        }

        Random random = new();
        await ctx.RespondAsync(
            new DiscordInteractionResponseBuilder().WithContent(
                $"Your random number is **{random.NextInt64(min, max)}**!"));
    }

    [Command("fact")]
    [Description("Get a random fact.")]
    public static async Task RandomFact(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        var fact = JsonConvert.DeserializeObject<JObject>(await Program.HttpClient.GetStringAsync("https://uselessfacts.jsph.pl/random.md?language=en"));
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(fact["text"].ToString()));
    }

    [Command("cat")]
    [Description("Get a random cat picture from the internet.")]
    public static async Task RandomCat(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        string data;
        try
        {
            data = await Program.HttpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You're going too fast! Try again in a few seconds."));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"Something went wrong! The Cat API returned status code {ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists."));

            return;
        }
        var pattern = CatApiUrlPattern();
        var cat = pattern.Match(data);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"{cat}"));
    }

    [Command("dog")]
    [Description("Get a random dog picture from the internet.")]
    public static async Task RandomDog(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        string data;
        try
        {
            data = await Program.HttpClient.GetStringAsync("https://dog.ceo/api/breeds/image/random");
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You're going too fast! Try again in a few seconds."));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"Something went wrong! The Dog API returned status code {ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists."));
                
            return;
        }
        var pattern = DogApiUrlPattern();
        var dogMatch = pattern.Match(data);

        var dog = dogMatch.ToString();
        dog = dog.Replace("\\", "");
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"{dog}"));
    }

    [GeneratedRegex(@"https:\/\/cdn2.thecatapi.com\/images\/.*(\.[A-Za-z]{3,4})")]
    private static partial Regex CatApiUrlPattern();
    [GeneratedRegex(@"https:\\\/\\\/images.dog.ceo\\\/breeds\\\/.*(\.[A-Za-z]{3,4})")]
    private static partial Regex DogApiUrlPattern();
}
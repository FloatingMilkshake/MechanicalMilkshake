namespace MechanicalMilkshake.Commands;

[Command("random")]
[Description("Get a random number, fact, or picture of a dog or cat.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
internal class RandomCommands
{
    [Command("number")]
    [Description("Generates a random number between two that you specify.")]
    public static async Task RandomNumberCommandAsync(SlashCommandContext ctx,
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
    public static async Task RandomFactCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        Setup.Types.Apis.FactApi.Fact fact;
        try
        {
            var data = await Setup.Constants.HttpClient.GetStringAsync("https://uselessfacts.jsph.pl/api/v2/facts/random?language=en");
            fact = JsonConvert.DeserializeObject<Setup.Types.Apis.FactApi.Fact>(data);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You're going too fast! Try again in a few seconds."));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"Something went wrong! Error code  {(int)ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists."));

            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(fact.Text));
    }

    [Command("cat")]
    [Description("Get a random cat picture from the internet.")]
    public static async Task RandomCatCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        List<Setup.Types.Apis.CatDogApi.CatDogImage> images;
        try
        {
            var data = await Setup.Constants.HttpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
            images = JsonConvert.DeserializeObject<List<Setup.Types.Apis.CatDogApi.CatDogImage>>(data);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You're going too fast! Try again in a few seconds."));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"Something went wrong! Error code {(int)ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists."));

            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(images.First().Url));
    }

    [Command("dog")]
    [Description("Get a random dog picture from the internet.")]
    public static async Task RandomDogCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync();
        List<Setup.Types.Apis.CatDogApi.CatDogImage> images;
        try
        {
            var data = await Setup.Constants.HttpClient.GetStringAsync("https://api.thedogapi.com/v1/images/search");
            images = JsonConvert.DeserializeObject<List<Setup.Types.Apis.CatDogApi.CatDogImage>>(data);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You're going too fast! Try again in a few seconds."));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"Something went wrong! Error code {(int)ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists."));

            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(images.First().Url));
    }
}
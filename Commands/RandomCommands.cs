namespace MechanicalMilkshake.Commands;

[Command("random")]
[Description("Get a random number, fact, or picture of a dog or cat.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
[InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
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
                new DiscordInteractionResponseBuilder()
                .WithContent("The minimum number cannot be greater than the maximum number!")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            return;
        }

        Random random = new();
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"Your random number is **{random.NextInt64(min, max)}**!")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("fact")]
    [Description("Get a random fact.")]
    public static async Task RandomFactCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        FactApiResponse fact;
        try
        {
            var data = await Setup.Constants.HttpClient.GetStringAsync("https://uselessfacts.jsph.pl/api/v2/facts/random?language=en");
            fact = JsonConvert.DeserializeObject<FactApiResponse>(data);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You're going too fast! Try again in a few seconds.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Something went wrong! Error code {(int)ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));

            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent(fact.Text)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("cat")]
    [Description("Get a random cat picture from the internet.")]
    public static async Task RandomCatCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        List<CatDogApiResponse> images;
        try
        {
            var data = await Setup.Constants.HttpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
            images = JsonConvert.DeserializeObject<List<CatDogApiResponse>>(data);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You're going too fast! Try again in a few seconds.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Something went wrong! Error code {(int)ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));

            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent(images.First().Url)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    [Command("dog")]
    [Description("Get a random dog picture from the internet.")]
    public static async Task RandomDogCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        List<CatDogApiResponse> images;
        try
        {
            var data = await Setup.Constants.HttpClient.GetStringAsync("https://api.thedogapi.com/v1/images/search");
            images = JsonConvert.DeserializeObject<List<CatDogApiResponse>>(data);
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode is HttpStatusCode.TooManyRequests)
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You're going too fast! Try again in a few seconds.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            else
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent($"Something went wrong! Error code {(int)ex.StatusCode}. Try again in a bit, or contact a bot owner if this persists.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));

            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent(images.First().Url)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }

    internal class CatDogApiResponse
    {
        [JsonProperty("id")] internal string Id { get; private set; }
        [JsonProperty("url")] internal string Url { get; private set; }
        [JsonProperty("width")] internal int Width { get; private set; }
        [JsonProperty("height")] internal int Height { get; private set; }
    }

    internal class FactApiResponse
    {
        [JsonProperty("id")] internal string Id { get; private set; }
        [JsonProperty("text")] internal string Text { get; private set; }
        [JsonProperty("source")] internal string Source { get; private set; }
        [JsonProperty("source_url")] internal string SourceUrl { get; private set; }
        [JsonProperty("language")] internal string Language { get; private set; }
        [JsonProperty("permalink")] internal string Permalink { get; private set; }
    }
}

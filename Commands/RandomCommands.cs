namespace MechanicalMilkshake.Commands;

public partial class RandomCommands : ApplicationCommandModule
{
    [SlashCommandGroup("random", "Get a random number, fact, or picture of a dog or cat.")]
    private partial class RandomCmds : ApplicationCommandModule
    {
        [SlashCommand("number", "Generates a random number between two that you specify.")]
        public static async Task RandomNumber(InteractionContext ctx,
            [Option("min", "The minimum number to choose between. Defaults to 1.")]
            long min = 1,
            [Option("max", "The maximum number to choose between. Defaults to 10.")]
            long max = 10)
        {
            if (min > max)
            {
                await ctx.CreateResponseAsync(
                    new DiscordInteractionResponseBuilder().WithContent(
                        "The minimum number cannot be greater than the maximum number!"));
                return;
            }

            Random random = new();
            await ctx.CreateResponseAsync(
                new DiscordInteractionResponseBuilder().WithContent(
                    $"Your random number is **{random.NextInt64(min, max)}**!"));
        }

        [SlashCommand("fact", "Get a random fact.")]
        public static async Task RandomFact(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var fact = JsonConvert.DeserializeObject<JObject>(await Program.HttpClient.GetStringAsync("https://uselessfacts.jsph.pl/random.md?language=en"));
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(fact["text"].ToString()));
        }

        [SlashCommand("cat", "Get a random cat picture from the internet.")]
        public static async Task RandomCat(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var data = await Program.HttpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
            var pattern = CatApiUrlPattern();
            var cat = pattern.Match(data);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"{cat}"));
        }

        [SlashCommand("dog", "Get a random dog picture from the internet.")]
        public static async Task RandomDog(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var data = await Program.HttpClient.GetStringAsync("https://dog.ceo/api/breeds/image/random");
            var pattern = DogApiUrlPattern();
            var dogMatch = pattern.Match(data);

            var dog = dogMatch.ToString();
            dog = dog.Replace("\\", "");
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"{dog}"));
        }

        [GeneratedRegex(@"https:\/\/cdn2.thecatapi.com\/images\/.*(\.[A-Za-z]{3,4})")]
        private static partial Regex CatApiUrlPattern();
        [GeneratedRegex(@"https:\\\/\\\/images.dog.ceo\\\/breeds\\\/.*(\.[A-Za-z]{3,4})")]
        private static partial Regex DogApiUrlPattern();
    }
}
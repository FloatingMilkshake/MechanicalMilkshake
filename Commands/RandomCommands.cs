namespace MechanicalMilkshake.Commands;

public class RandomCommands : ApplicationCommandModule
{
    [SlashCommandGroup("random", "Get a random number, fact, or picture of a dog or cat.")]
    private class RandomCmds : ApplicationCommandModule
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

            var fact = await Program.HttpClient.GetStringAsync("https://uselessfacts.jsph.pl/random.md?language=en");

            if (fact.Contains("http"))
            {
                fact = fact.Replace(")", ">)");
                fact = fact.Replace("http", "<http");
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(fact));
        }

        [SlashCommand("cat", "Get a random cat picture from the internet.")]
        public static async Task RandomCat(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var data = await Program.HttpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
            Regex pattern = new(@"https:\/\/cdn2.thecatapi.com\/images\/.*(.png|.jpg)");
            var cat = pattern.Match(data);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"{cat}"));
        }

        [SlashCommand("dog", "Get a random dog picture from the internet.")]
        public static async Task RandomDog(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
            var data = await Program.HttpClient.GetStringAsync("https://dog.ceo/api/breeds/image/random");
            Regex pattern = new(@"https:\\\/\\\/images.dog.ceo\\\/breeds\\\/.*(.png|.jpg)");
            var dogMatch = pattern.Match(data);

            var dog = dogMatch.ToString();
            dog = dog.Replace("\\", "");
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"{dog}"));
        }
    }
}
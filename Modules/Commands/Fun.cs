namespace MechanicalMilkshake.Modules.Commands
{
    public class Fun : ApplicationCommandModule
    {
        [SlashCommandGroup("random", "Get a random number, fact, or picture of a dog or cat.")]
        class RandomCmds : ApplicationCommandModule
        {
            [SlashCommand("number", "Generates a random number between two that you specify.")]
            public async Task RandomNumber(InteractionContext ctx, [Option("min", "The minimum number to choose between. Defaults to 1.")] long min = 1, [Option("max", "The maximum number to choose between. Defaults to 10.")] long max = 10)
            {
                if (min > max)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("The minimum number cannot be greater than the maximum number!"));
                    return;
                }

                Random random = new();
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Your random number is **{random.Next((int)min, (int)max)}**!"));
            }

            [SlashCommand("fact", "Get a random fact.")]
            public async Task RandomFact(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("*Getting a random fact...*"));

                string fact = await Program.httpClient.GetStringAsync("https://uselessfacts.jsph.pl/random.md");

                if (fact.Contains("http"))
                {
                    fact = fact.Replace(")", ">)");
                    fact = fact.Replace("http", "<http");
                }

                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(fact));
            }

            [SlashCommand("cat", "Get a random cat picture from the internet.")]
            public async Task RandomCat(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("*Looking for a cat...*"));
                string data = await Program.httpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
                Regex pattern = new(@"https:\/\/cdn2.thecatapi.com\/images\/.*(.png|.jpg)");
                Match cat = pattern.Match(data);
                if (cat is not null)
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{cat}"));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I found a cat, but something happened and I wasn't able to send it here. Try again."));
                }
            }

            [SlashCommand("dog", "Get a random dog picture from the internet.")]
            public async Task RandomDog(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("*Looking for a dog...*"));
                string data = await Program.httpClient.GetStringAsync("https://dog.ceo/api/breeds/image/random");
                Regex pattern = new(@"https:\\\/\\\/images.dog.ceo\\\/breeds\\\/.*(.png|.jpg)");
                Match dogMatch = pattern.Match(data);
                if (dogMatch is not null)
                {
                    string dog = dogMatch.ToString();
                    dog = dog.Replace("\\", "");
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"{dog}"));
                }
                else
                {
                    await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("I found a dog, but something happened and I wasn't able to send it here. Try again."));
                }
            }
        }
    }
}
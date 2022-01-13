using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
    public class Fun : BaseCommandModule
    {
        [Group("random")]
        [Description("Get a random number, fact, or picture of a dog or cat.")]
        class RandomCmds : BaseCommandModule
        {
            [Command("number")]
            [Description("Generates a random number between two that you specify.")]
            public async Task RandomNumber(CommandContext ctx, [Description("The minimum number to choose between. Defaults to 1.")] int min = 1, [Description("The maximum number to choose between. Defaults to 10.")] int max = 10)
            {
                if (min > max)
                {
                    await ctx.RespondAsync("The minimum number cannot be greater than the maximum number!");
                    return;
                }

                Random random = new();
                await ctx.RespondAsync($"Your random number is **{random.Next(min, max)}**!");
            }

            [Command("fact")]
            [Description("Get a random fact.")]
            public async Task RandomFact(CommandContext ctx)
            {
                DSharpPlus.Entities.DiscordMessage msg = await ctx.RespondAsync("*Getting a random fact...*");

                string fact = await Program.httpClient.GetStringAsync("https://uselessfacts.jsph.pl/random.md");

                if (fact.Contains("http"))
                {
                    fact = fact.Replace(")", ">)");
                    fact = fact.Replace("http", "<http");
                }

                await msg.ModifyAsync(fact);
            }

            [Command("cat")]
            [Description("Get a random cat picture from the internet.")]
            public async Task RandomCat(CommandContext ctx)
            {
                DSharpPlus.Entities.DiscordMessage msg = await ctx.RespondAsync("*Looking for a cat...*");
                string data = await Program.httpClient.GetStringAsync("https://api.thecatapi.com/v1/images/search");
                Regex pattern = new(@"https:\/\/cdn2.thecatapi.com\/images\/.*(.png|.jpg)");
                Match cat = pattern.Match(data);
                if (cat is not null)
                {
                    await msg.ModifyAsync($"{cat}");
                }
                else
                {
                    await msg.ModifyAsync("I found a cat, but something happened and I wasn't able to send it here. Try again.");
                }
            }

            [Command("dog")]
            [Description("Get a random dog picture from the internet.")]
            public async Task RandomDog(CommandContext ctx)
            {
                DSharpPlus.Entities.DiscordMessage msg = await ctx.RespondAsync("*Looking for a dog...*");
                string data = await Program.httpClient.GetStringAsync("https://dog.ceo/api/breeds/image/random");
                Regex pattern = new(@"https:\\\/\\\/images.dog.ceo\\\/breeds\\\/.*(.png|.jpg)");
                Match dogMatch = pattern.Match(data);
                if (dogMatch is not null)
                {
                    string dog = dogMatch.ToString();
                    dog = dog.Replace("\\", "");
                    await msg.ModifyAsync($"{dog}");
                }
                else
                {
                    await msg.ModifyAsync("I found a dog, but something happened and I wasn't able to send it here. Try again.");
                }
            }
        }

        [Group("letter")]
        [Description("Repeat a single letter, or many.")]
        public class LetterCmds : BaseCommandModule
        {
            [Command("spam")]
            [Aliases("smash", "keyboardsmash")]
            [Description("Spam letters. Think of a keyboard smash.")]
            public async Task LetterSpam(CommandContext ctx, [Description("The number of letters.")] int size)
            {
                const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                string kbSmash = new(Enumerable.Repeat(chars, size).Select(s => s[Program.random.Next(s.Length)]).ToArray());
                await ctx.RespondAsync(kbSmash);
            }

            [Command("repeat")]
            [Description("Repeat a single letter.")]
            public async Task LetterRepeat(CommandContext ctx, [Description("The letter to repeat.")] string letter, [Description("The number of times to repeat it.")] int count)
            {
                if (letter.Length > 1)
                {
                    await ctx.RespondAsync("Only one letter can be repeated.");
                    return;
                }
                if (count > 2000)
                {
                    await ctx.RespondAsync("Character limit exceeded! Messages can only be up to 2000 characters in size.");
                }

                string letterSpam = new(Enumerable.Repeat(letter, count).Select(s => s[Program.random.Next(s.Length)]).ToArray());
                await ctx.RespondAsync(letterSpam);
            }
        }
    }
}
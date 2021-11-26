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
        [Command("hi")]
        [Description("Says hi back to you!")]
        public async Task Greeting(CommandContext ctx)
        {
            await ctx.RespondAsync($"hi {ctx.Member.Mention}!");
        }

        [Command("randomnumber")]
        [Description("Generates a random number between two that you specify.")]
        public async Task Random(CommandContext ctx, [Description("The minimum number to choose between. Defaults to 1.")] int min = 1, [Description("The maximum number to choose between. Defaults to 10.")] int max = 10)
        {
            if (min > max)
            {
                await ctx.RespondAsync("The minimum number cannot be greater than the maximum number!");
                return;
            }

            Random random = new();
            await ctx.RespondAsync($"Your random number is **{random.Next(min, max)}**!");
        }

        [Command("type")]
        [Description("Makes the bot type.")]
        public async Task TypeCommand(CommandContext ctx)
        {
            try
            {
                if (!ctx.Channel.IsPrivate)
                {
                    await ctx.Message.DeleteAsync();
                }
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                // Fail silently (do nothing). The bot cannot delete the invoking message, which is fine. There's no response here to avoid clutter.
            }
            await ctx.TriggerTypingAsync();
        }

        [Command("cat")]
        [Description("Gets a random cat picture from the internet.")]
        public async Task Cat(CommandContext ctx)
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
        [Description("Gets a random dog picture from the internet.")]
        public async Task Dog(CommandContext ctx)
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

        [Command("randomfact")]
        [Description("Gets a random fact.")]
        public async Task Fact(CommandContext ctx)
        {
            DSharpPlus.Entities.DiscordMessage msg = await ctx.RespondAsync("*Getting a random fact...*");

            string fact = await Program.httpClient.GetStringAsync("https://uselessfacts.jsph.pl/random.md");

            fact = fact.Replace(")", ">)");
            fact = fact.Replace("http", "<http");

            await msg.ModifyAsync(fact);
        }

        [Command("edit")]
        [Description("Sends a message that is edited to another message you specify after a given time in seconds.")]
        public async Task Edit(CommandContext ctx, [Description("Initial message for the bot to send.")] string message, [Description("What the message should be edited to.")] string editedMessage, [Description("How long the bot should wait to edit the message, in seconds.")] int delay)
        {
            if (delay is 0)
            {
                await ctx.RespondAsync("You must specify a time to wait before editing the message!");
            }
            else
            {
                try
                {
                    if (!ctx.Channel.IsPrivate)
                    {
                        await ctx.Message.DeleteAsync();
                    }
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    // Fail silently (do nothing). The bot cannot delete the invoking message, which is fine. There's no response here to avoid clutter.
                }

                DSharpPlus.Entities.DiscordMessage msg = await ctx.Channel.SendMessageAsync(message);
                await Task.Delay(delay * 1000);
                await msg.ModifyAsync(editedMessage);
            }
        }

        [Command("keyboardsmash")]
        [Aliases("mashy-mashy", "kbsmash", "mashymashy", "smash")]
        [Description("Keyboard smash.")]
        public async Task KeyboardSmash(CommandContext ctx, [Description("The number of letters in the keyboard smash.")] int size)
        {
            await ctx.TriggerTypingAsync();
            await Task.Delay(3000);
            const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string kbSmash = new(Enumerable.Repeat(chars, size).Select(s => s[Program.random.Next(s.Length)]).ToArray());
            await ctx.RespondAsync(kbSmash);
        }

        [Command("letterspam")]
        [Aliases("letters", "repeatletter")]
        [Description("Repeats a letter.")]
        public async Task LetterSpam(CommandContext ctx, [Description("The letter to repeat.")] string letter, [Description("The number of times to repeat the letter.")] int count)
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

            await ctx.TriggerTypingAsync();
            await Task.Delay(3000);

            string letterSpam = new(Enumerable.Repeat(letter, count).Select(s => s[Program.random.Next(s.Length)]).ToArray());
            await ctx.RespondAsync(letterSpam);
        }
    }
}
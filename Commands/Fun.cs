using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;

namespace DiscordBot.Commands
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
        public async Task Random(CommandContext ctx, int min, int max)
        {
            if (min > max)
            {
                await ctx.RespondAsync("Error: Mimimum value cannot be greater than maximum value!");
            }
            var random = new Random();
            await ctx.RespondAsync($"Your random number is **{random.Next(min, max)}**!");
        }

        [Command("type")]
        [Description("Makes the bot type.")]
        public async Task TypeCommand(CommandContext ctx)
        {
            await ctx.Message.DeleteAsync();
            await ctx.TriggerTypingAsync();
        }

        [Command("cat")]
        [Description("Gets a random cat picture from the internet.")]
        public async Task Cat(CommandContext ctx)
        {
            var msg = await ctx.RespondAsync("*Looking for a cat...*");
            var cli = new WebClient();
            string data = cli.DownloadString("https://api.thecatapi.com/v1/images/search");
            Regex pattern = new Regex(@"https:\/\/cdn2.thecatapi.com\/images\/.*(.png|.jpg)");
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
            var msg = await ctx.RespondAsync("*Looking for a dog...*");
            var cli = new WebClient();
            string data = cli.DownloadString("https://dog.ceo/api/breeds/image/random");
            Regex pattern = new Regex(@"https:\\\/\\\/images.dog.ceo\\\/breeds\\\/.*(.png|.jpg)");
            Match dogMatch = pattern.Match(data);
            if (dogMatch is not null)
            {
                var dog = dogMatch.ToString();
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
        public async Task fact(CommandContext ctx)
        {
            var msg = await ctx.RespondAsync("*Getting a random fact...*");

            var cli = new WebClient();
            string fact = cli.DownloadString("https://uselessfacts.jsph.pl/random.md");

            fact = fact.Replace(")", ">)");
            fact = fact.Replace("http", "<http");

            await msg.ModifyAsync(fact);
        }

        [Command("edit")]
        [Description("Sends a message that is edited to another message you specify after a given time in seconds.")]
        public async Task Edit(CommandContext ctx, string message, string editedMessage, int delay)
        {
            if (delay is 0)
            {
                await ctx.RespondAsync("You must specify a time to wait before editing the message!");
            }
            else
            {
                await ctx.Message.DeleteAsync();
                var msg = await ctx.Channel.SendMessageAsync(message);
                await Task.Delay(delay * 1000);
                await msg.ModifyAsync(editedMessage);
            }
        }

    }
}
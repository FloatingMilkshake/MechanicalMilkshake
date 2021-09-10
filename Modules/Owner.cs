using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace DiscordBot.Modules
{
    public class Owner : BaseCommandModule
    {
        [Command("tellraw")]
        [Description("**Owner only:** Speak through the bot!")]
        public async Task Tellraw(CommandContext ctx, [Description("The message to have the bot send."), RemainingText] string message)
        {
            if (ctx.Message.Author.Username != "FloatingMilkshake" && ctx.Message.Author.Discriminator != "7777")
            {
                await ctx.RespondAsync("You don't have permission to perform this command!\n`tellraw` is an owner-only command.");
                return;
            }
            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync(message);
        }

        [Command("shutdown")]
        [Description("**Owner only:** Shuts down the bot.")]
        public async Task Shutdown(CommandContext ctx, [Description("This must be \"I am sure\" for the command to run."), RemainingText] string areYouSure)
        {
            if (ctx.Message.Author.Username != "FloatingMilkshake" && ctx.Message.Author.Discriminator != "7777")
            {
                await ctx.RespondAsync("You don't have permission to perform this command!\n`shutdown` is an owner-only command.");
                return;
            }
            if (areYouSure == "I am sure")
            {
                await ctx.RespondAsync("**Warning**: The bot is now shutting down.");
                Environment.Exit(0);
            }
            else
            {
                await ctx.RespondAsync("Are you sure?");
            }
        }
    }
}

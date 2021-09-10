using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class Owner : BaseCommandModule
    {
        [Command("tellraw")]
        [Description("**Owner only:** Speak through the bot!")]
        [RequireOwner]
        public async Task Tellraw(CommandContext ctx, [Description("The message to have the bot send."), RemainingText] string message)
        {
            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync(message);
        }

        [Command("shutdown")]
        [Description("**Owner only:** Shuts down the bot.")]
        [RequireOwner]
        public async Task Shutdown(CommandContext ctx, [Description("This must be \"I am sure\" for the command to run."), RemainingText] string areYouSure)
        {
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

using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class Owner : BaseCommandModule
    {
        [Command("shutdown")]
        [Description("**Owner-only:** Shuts down the bot.")]
        [RequireOwner]
        public async Task Shutdown(CommandContext ctx, [Description("This must be \"I am sure\" for the command to run."), RemainingText] string areYouSure)
        {
            if (areYouSure == "I am sure")
            {
                var msg = await ctx.RespondAsync("**Warning**: The bot is now shutting down. This action is permanent."
                    + "\nDisconnecting from websocket...");
                await ctx.Client.DisconnectAsync();
                await msg.ModifyAsync("**Warning**: The bot is now shutting down. This action is permanent.");
                Environment.Exit(0);
            }
            else
            {
                await ctx.RespondAsync("Are you sure?");
            }
        }
    }
}

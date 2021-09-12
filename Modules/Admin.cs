using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class Admin : BaseCommandModule
    {
        [Command("tellraw")]
        [Description("**Admin-only:** Speak through the bot!")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task Tellraw(CommandContext ctx, [Description("The message to have the bot send."), RemainingText] string message)
        {
            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync(message);
        }

        [Command("shutdown")]
        [Description("**Admin-only:** Shuts down the bot.")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task Shutdown(CommandContext ctx, [Description("This must be \"I am sure\" for the command to run."), RemainingText] string areYouSure)
        {
            if (areYouSure == "I am sure")
            {
                await ctx.RespondAsync("**Warning**: The bot is now shutting down. This action is permanent.");
                Environment.Exit(0);
            }
            else
            {
                await ctx.RespondAsync("Are you sure?");
            }
        }

        [Command("restart")]
        [Description("**Admin-only:** Restarts the bot.")]
        [RequirePermissions(Permissions.Administrator)]
        public async Task Restart(CommandContext ctx)
        {
            string dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
            if (string.IsNullOrWhiteSpace(dockerCheckFile))
            {
                await ctx.RespondAsync("The bot may not be running under Docker; this means that `!restart` will behave like `!shutdown`."
                    + "\n\nAborted. Use `!shutdown` if you wish to shut down the bot.");
                return;
            }

            Environment.Exit(1);
        }
    }
}

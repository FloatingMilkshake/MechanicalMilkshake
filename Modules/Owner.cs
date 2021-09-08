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
        public async Task Tellraw(CommandContext ctx, [Description("The message to have the bot send.")][RemainingText] string message)
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
        public async Task Shutdown(CommandContext ctx, [Description("This must be \"I am sure\" for the command to run.")] [RemainingText] string areYouSure)
        {
            if (ctx.Message.Author.Username != "FloatingMilkshake" && ctx.Message.Author.Discriminator != "7777")
            {
                await ctx.RespondAsync("You don't have permission to perform this command!\n`shutdown` is an owner-only command.");
                return;
            }
            if (areYouSure == "I am sure")
            {
                await ctx.RespondAsync("**Warning**: The bot is now shutting down. If running in a debug session in Visual Studio, it will exit.");
                Environment.Exit(0);
            }
            else
            {
                await ctx.RespondAsync("Are you sure?");
            }
        }

        [Command("pwsh")]
        [Description("**Owner only:** Runs a PowerShell command on the host machine.")]
        public async Task pwsh(CommandContext ctx, [Description("The PowerShell command to run.")] [RemainingText] String args)
        {
            if (ctx.Message.Author.Username != "FloatingMilkshake" && ctx.Message.Author.Discriminator != "7777")
            {
                await ctx.RespondAsync("You don't have permission to perform this command!\n`pwsh` is an owner-only command.");
                return;
            }
            var msg = await ctx.Channel.SendMessageAsync($"*Executing...*");

            var pwshTemp = "discordBotTemp.ps1";

            await File.WriteAllTextAsync(pwshTemp, args);

            var start = new ProcessStartInfo
            {
                FileName = "pwsh.exe",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                Arguments = pwshTemp,
                CreateNoWindow = true
            };

            var process = Process.Start(start);
            var reader = process.StandardOutput;

            process.EnableRaisingEvents = true;

            var output = reader.ReadToEnd();

            await process.WaitForExitAsync();

            if (output.Contains("is not recognized as a name of a cmdlet, function, script file, or executable"))
            {
                output = "The command you entered was not recognized. Try again!";
            }

            if (output is null or "")
            {
                output = "There was no output for this command.";
            }

            await msg.ModifyAsync($"```\n{output}\n```");

            File.Delete(pwshTemp);
        }
    }
}
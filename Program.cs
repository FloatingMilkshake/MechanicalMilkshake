using DiscordBot.Modules;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        internal static async Task MainAsync()
        {
            if (Environment.GetEnvironmentVariable("BOT_TOKEN") == null)
            {
                Console.WriteLine("ERROR: No token provided! Make sure the environment variable BOT_TOKEN is set.");
                Environment.Exit(1);
            }

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "!" }
            });

            async Task CommandsNextService_CommandErrored(CommandsNextExtension cnext, CommandErrorEventArgs e)
            {
                if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                    return;

                var exs = new List<Exception>();
                if (e.Exception is AggregateException ae)
                    exs.AddRange(ae.InnerExceptions);
                else
                    exs.Add(e.Exception);

                foreach (var ex in exs)
                {
                    if (ex is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                        return;

                    if (ex is ChecksFailedException && (e.Command.Name != "help"))
                        return;

                    var embed = new DiscordEmbedBuilder
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "An exception occurred when executing a command",
                        Description = $"`{e.Exception.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("Message", ex.Message);
                    if (e.Exception.GetType().ToString() == "System.ArgumentException")
                        embed.AddField("Note", "This usually means that you used the command incorrectly.\n" +
                            "Please double-check how to use this command.");
                    await e.Context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
                }
            }

            commands.RegisterCommands<Owner>();
            commands.RegisterCommands<Utility>();
            commands.RegisterCommands<Fun>();
            commands.CommandErrored += CommandsNextService_CommandErrored;

            await discord.ConnectAsync();

            var general = await discord.GetChannelAsync(882446411130601472);
            await general.SendMessageAsync("Connected!");

            await Task.Delay(-1);
        }
    }
}

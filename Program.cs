using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using MechanicalMilkshake.Modules;
using Minio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace MechanicalMilkshake
{
    class Program
    {
        public static DiscordClient discord;
        public static MinioClient minio;
        public static Random random = new();
        public static DateTime connectTime;
        public static HttpClient httpClient = new();

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

            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = Environment.GetEnvironmentVariable("BOT_TOKEN"),
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });
            CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "!", "~" }
            });

            minio = new MinioClient
            (
                Environment.GetEnvironmentVariable("S3_ENDPOINT"),
                Environment.GetEnvironmentVariable("S3_ACCESS_KEY"),
                Environment.GetEnvironmentVariable("S3_SECRET_KEY"),
                Environment.GetEnvironmentVariable("S3_REGION")
            ).WithSSL();

            async Task CommandsNextService_CommandErrored(CommandsNextExtension cnext, CommandErrorEventArgs e)
            {
                if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                    return;

                List<Exception> exs = new();
                if (e.Exception is AggregateException ae)
                    exs.AddRange(ae.InnerExceptions);
                else
                    exs.Add(e.Exception);

                foreach (Exception ex in exs)
                {
                    if (ex is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                        return;

                    if (ex is ChecksFailedException && (e.Command.Name != "help"))
                        return;

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "An exception occurred when executing a command",
                        Description = $"`{e.Exception.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("Message", ex.Message);
                    if (e.Exception.GetType().ToString() == "System.ArgumentException")
                    {
                        embed.AddField("What's that mean?", "This usually means that you used the command incorrectly.\n" +
                            $"Please run `help {e.Command.QualifiedName}` for information about what you need to provide for the `{e.Command.QualifiedName}` command.");
                    }
                    if (e.Command.QualifiedName == "upload")
                    {
                        embed.AddField("Did you forget to include a file name?", "`upload` requires that you specify a name for the file as the first argument. If you'd like to use a randomly-generated file name, use the name `random`.");
                    }
                    await e.Context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
                }
            }

            commands.RegisterCommands<Owner>();
            commands.RegisterCommands<Utility>();
            commands.RegisterCommands<Fun>();
            commands.RegisterCommands<Mod>();
            commands.RegisterCommands<PerServerFeatures>();
            commands.CommandErrored += CommandsNextService_CommandErrored;

            if (Environment.GetEnvironmentVariable("HOME_CHANNEL") == null)
            {
                Console.WriteLine("ERROR: No home channel provided! Make sure the environment variable HOME_CHANNEL is set.");
                Environment.Exit(1);
            }

            string homeEnvVar = Environment.GetEnvironmentVariable("HOME_CHANNEL");
            ulong home = Convert.ToUInt64(homeEnvVar);
            DiscordChannel homeChannel = await discord.GetChannelAsync(home);

            string commitHash = "";
            if (File.Exists("CommitHash.txt"))
            {
                StreamReader readHash = new("CommitHash.txt");
                commitHash = readHash.ReadToEnd();
            }
            if (commitHash == "")
            {
                commitHash = "dev";
            }

            string commitMessage = "";
            if (File.Exists("CommitMessage.txt"))
            {
                StreamReader readMessage = new("CommitMessage.txt");
                commitMessage = readMessage.ReadToEnd();
            }
            if (commitMessage == "")
            {
                commitMessage = "No commit message is available when debugging.";
            }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            async Task OnReady(DiscordClient client, ReadyEventArgs e)
            {
                Task.Run(async () =>
                {
                    connectTime = DateTime.Now;

                    await homeChannel.SendMessageAsync($"Connected! Latest commit: `{commitHash}`"
                        + $"\nLatest commit message:\n```\n{commitMessage}\n```");
                });
            }

            await discord.ConnectAsync();
            discord.Ready += OnReady;

            while (true)
            {
                await Task.Delay(10000);
                PerServerFeatures.WednesdayCheck();
            }
        }
    }
}

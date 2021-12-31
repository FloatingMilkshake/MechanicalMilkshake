using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using DSharpPlus.Interactivity.Extensions;
using MechanicalMilkshake.Modules;
using Minio;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
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
        public static ConfigJson configjson;

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        internal static async Task MainAsync()
        {
            var json = "";

            string configFile = "config.json";
#if DEBUG
            configFile = "config.dev.json";
#endif

            using (var fs = File.OpenRead(configFile))
            using (var sr = new StreamReader(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();

            configjson = JsonConvert.DeserializeObject<ConfigJson>(json);

            if (configjson.BotToken == null || configjson.BotToken == "")
            {
                Console.WriteLine("ERROR: No token provided! Make sure the botToken field in your config.json file is set.");
                Environment.Exit(1);
            }

            discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = configjson.BotToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });
            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });

            CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "!", "~", "-", "mm" }
            });

            minio = new MinioClient
            (
                configjson.S3.Endpoint,
                configjson.S3.AccessKey,
                configjson.S3.SecretKey,
                configjson.S3.Region
            ).WithSSL();

            // Most of the code for this exception handler is from Erisa's Cliptok: https://github.com/Erisa/Cliptok/blob/main/Program.cs#L481-L520

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

                    if (ex is ChecksFailedException)
                    {
                        await e.Context.RespondAsync("Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up.");
                        return;
                    }

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "An exception occurred when executing a command",
                        Description = $"`{ex.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("Message", ex.Message);
                    if (ex.GetType().ToString() == "System.ArgumentException")
                    {
                        embed.AddField("What's that mean?", "This usually means that you used the command incorrectly.\n" +
                            $"Please run `help {e.Command.QualifiedName}` for information about what you need to provide for the `{e.Command.QualifiedName}` command.");
                    }
                    if (e.Command.QualifiedName == "upload")
                    {
                        embed.AddField("Did you forget to include a file name?", "`upload` requires that you specify a name for the file as the first argument. If you'd like to use a randomly-generated file name, use the name `random`.");
                    }
                    if (e.Command.QualifiedName == "tellraw" && ex.GetType().ToString() == "System.ArgumentException")
                    {
                        await e.Context.RespondAsync("An error occurred! Either you did not specify a target channel, or I do not have permission to see that channel.");
                    }
                    if (ex.GetType().ToString() == "System.InvalidOperationException" && ex.Message.Contains("this group is not executable"))
                    {
                        await e.Context.RespondAsync($"Did you mean to run a command inside this group? `{e.Command.QualifiedName}` cannot be run on its own. Try `help {e.Command.QualifiedName}` to see what commands are available.");
                    }
                    else
                    {
                        await e.Context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
                    }
                }
            }

            commands.RegisterCommands<Owner>();
            commands.RegisterCommands<Utility>();
            commands.RegisterCommands<Fun>();
            commands.RegisterCommands<Mod>();
            commands.RegisterCommands<PerServerFeatures>();
            commands.CommandErrored += CommandsNextService_CommandErrored;

            if (configjson.HomeChannel == null)
            {
                Console.WriteLine("ERROR: No home channel provided! Make sure the homeChannel field in your config.json file is set.");
                Environment.Exit(1);
            }

            ulong homeChannelId = Convert.ToUInt64(configjson.HomeChannel);

            DiscordChannel homeChannel = await discord.GetChannelAsync(homeChannelId);

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
                commitMessage = $"Process started at {DateTime.Now}";
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
                PerServerFeatures.WednesdayCheck();
                await Task.Delay(60000);
            }
        }
    }
}

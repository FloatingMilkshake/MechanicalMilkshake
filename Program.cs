using DiscordBot.Configuration;
using DiscordBot.Modules;
using System.Reflection;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using Minio;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace DiscordBot
{
    public class Bot
    {
        public static MinioClient minio;
        public static Random random = new Random();
        public static ConfigJson Config = new ConfigJson();

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        internal static async Task MainAsync()
        {
            // ConfigJson
            string json = await File.ReadAllTextAsync("Configuration/Configuration.json");
            using (var fs = File.OpenRead("Configuration/Configuration.json")) Config = JsonConvert.DeserializeObject<ConfigJson>(json);
            var configJson = JsonConvert.DeserializeObject<ConfigJson>(json);

            if (configJson.Token == null)
            {
                Console.WriteLine("ERROR: No token provided! Make sure the environment variable BOT_TOKEN is set.");
                Environment.Exit(1);
            }

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = configJson.Token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "!", "~" }
            });

            minio = new MinioClient
            (
                configJson.S3_Endpoint,
                configJson.S3_Access_Key,
                configJson.S3_Secret_Key,
                configJson.S3_Region
            ).WithSSL();

            commands.RegisterCommands(Assembly.GetExecutingAssembly());
            commands.CommandErrored += Events.CommandEvents.CommandsNextService_CommandErrored;

            if (configJson.HomeChannel == null)
            {
                Console.WriteLine("ERROR: No home channel provided! Make sure the environment variable HOME_CHANNEL is set.");
                Environment.Exit(1);
            }

            var homeEnvVar = Environment.GetEnvironmentVariable("HOME_CHANNEL");
            ulong home = Convert.ToUInt64(homeEnvVar);
            var homeChannel = await discord.GetChannelAsync(home);

            String commitHash = "";
            if (File.Exists("CommitHash.txt"))
            {
                var readHash = new StreamReader("CommitHash.txt");
                commitHash = readHash.ReadToEnd();
            }
            if (commitHash == "")
            {
                commitHash = "dev";
            }

            String commitMessage = "";
            if (File.Exists("CommitMessage.txt"))
            {
                var readMessage = new StreamReader("CommitMessage.txt");
                commitMessage = readMessage.ReadToEnd();
            }
            if (commitMessage == "")
            {
                commitMessage = "No commit message is available when debugging.";
            }

            await discord.ConnectAsync();

            await homeChannel.SendMessageAsync($"Connected! Latest commit: `{commitHash}`"
                + $"\nLatest commit message:\n```\n{commitMessage}\n```");

            await Task.Delay(-1);
        }
    }
}

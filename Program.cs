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
        public static readonly string processStartTime = DateTime.Now.ToString();
        public static DiscordChannel homeChannel;

        // Use a custom help message to show that there are no CommandsNext commands since they
        // have all been migrated to slash commands
        public class CustomHelpFormatter : BaseHelpFormatter
        {
            protected DiscordEmbedBuilder _embed;

            public CustomHelpFormatter(CommandContext ctx) : base(ctx)
            {
                _embed = new DiscordEmbedBuilder();
            }

            public override BaseHelpFormatter WithCommand(Command command)
            {
                return this;
            }

            public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> cmds)
            {
                _embed.Title = "Help";
                _embed.Description = $"No commands are available with this prefix.\n\nAll commands have been moved to slash (`/`) commands. Type `/` and select {discord.CurrentUser.Username} to see available commands.";
                _embed.Color = DiscordColor.Red;

                return this;
            }

            public override CommandHelpMessage Build()
            {
                return new CommandHelpMessage(embed: _embed);
            }
        }

        internal static async Task Main(string[] args)
        {
            // Read config.json, or config.dev.json if running in development mode
            string json = "";
            string configFile = "config.json";
#if DEBUG
            configFile = "config.dev.json";
#endif
            using (FileStream fs = File.OpenRead(configFile))
            using (StreamReader sr = new(fs, new UTF8Encoding(false)))
                json = await sr.ReadToEndAsync();
            configjson = JsonConvert.DeserializeObject<ConfigJson>(json);

            // If the bot token is not set, we cannot continue. Display error and exit.
            if (configjson.BotToken == null || configjson.BotToken == "")
            {
                Console.WriteLine("ERROR: No token provided! Make sure the botToken field in your "
                    + "config.json file is set.");
                Environment.Exit(1);
            }

            // Configure Discord client and interactivity
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

            // Get home channel ID; if the home channel is not set, we cannot continue.
            // Display error and exit.
            if (configjson.HomeChannel == null)
            {
                Console.WriteLine("ERROR: No home channel provided! Make sure the homeChannel "
                    + "field in your config.json file is set.");
                Environment.Exit(1);
            }
            ulong homeChannelId = Convert.ToUInt64(configjson.HomeChannel);
            homeChannel = await discord.GetChannelAsync(homeChannelId);

            // Set up slash commands and CommandsNext
            SlashCommandsExtension slash = discord.UseSlashCommands();

            CommandsNextExtension commands = discord.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new[] { "!", "~", "-", "mm" }
            });

            // Use the custom help message set with CustomHelpFormatter
            commands.SetHelpFormatter<CustomHelpFormatter>();

            // Set up Minio (used for some Owner commands)
            minio = new MinioClient()
                .WithEndpoint(configjson.S3.Endpoint)
                .WithCredentials(configjson.S3.AccessKey, configjson.S3.SecretKey)
                .WithRegion(configjson.S3.Region)
                .WithSSL();

            try
            {
                // Register slash commands as guild commands in configured development server when
                // running in development mode
#if DEBUG
                slash.RegisterCommands<Owner>(configjson.DevServerId);
                slash.RegisterCommands<Owner.Private>(configjson.DevServerId);
                slash.RegisterCommands<Fun>(configjson.DevServerId);
                slash.RegisterCommands<Mod>(configjson.DevServerId);
                slash.RegisterCommands<Utility>(configjson.DevServerId);
                slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(configjson.DevServerId);
                Console.WriteLine("Slash commands registered for debugging.");

                // Register slash commands globally for 'production' bot
#else
            slash.RegisterCommands<Owner>();
            slash.RegisterCommands<Owner.Private>(configjson.DevServerId);
            slash.RegisterCommands<Fun>();
            slash.RegisterCommands<Mod>();
            slash.RegisterCommands<Utility>();
            Console.WriteLine("Slash commands registered globally.");
            
// Register slash commands for per-server features in respective servers
// & testing server for 'production' bot
            slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(631118217384951808);
            slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(984903591816990730);
            slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(configjson.DevServerId);
            slash.RegisterCommands<PerServerFeatures.RoleCommands>(984903591816990730);
#endif
            }
            catch (Exception ex)
            {
                DiscordButtonComponent restartButton = new(ButtonStyle.Danger, "Restart", "slash-fail-restart-button");

                string ownerMention = "";
                foreach (DiscordUser user in discord.CurrentApplication.Owners)
                {
                    ownerMention += user.Mention + " ";
                }

                DiscordMessageBuilder message = new()
                {
                    Content = $"{ownerMention.Trim()}\nSlash commands failed to register properly! Click the button to restart the bot. Exception details are below.\n```cs\n{ex.Message}\n```",
                };
                message.AddComponents(restartButton);

                await homeChannel.SendMessageAsync(message);
            }


            // Register CommandsNext commands
            commands.RegisterCommands<PerServerFeatures.MessageCommands>();

            await discord.ConnectAsync();

            // Events
            discord.Ready += Events.OnReady;
            discord.MessageCreated += Events.MessageCreated;
            discord.MessageUpdated += Events.MessageUpdated;
            discord.ComponentInteractionCreated += Events.ComponentInteractionCreated;
            commands.CommandErrored += Events.Errors.CommandsNextService_CommandErrored;
            slash.SlashCommandErrored += Events.Errors.SlashCommandErrored;

            /* Create an instance of the Owner.Private class and run a command to fix SSH key permissions
            at bot startup. I wanted to be able to do this somewhere else, but for now it seems
            like this is the best way of doing it that I'm aware of, and it works. */
            Owner.Private ownerPrivate = new();
            await ownerPrivate.RunCommand("cat /app/id_rsa > ~/.ssh/id_rsa && chmod 700 ~/.ssh/id_rsa");

            // Run checks

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Task.Run(async () =>
            {
                while (true)
                {
                    await Checks.PackageUpdateCheck();
                    await Task.Delay(21600000); // 6 hours
                }
            });

            while (true)
            {
                await Checks.PerServerFeatures.WednesdayCheck();
                await Checks.PerServerFeatures.PizzaTime();
                await Task.Delay(60000); // 1 minute
            }
        }
    }

    // Set custom command attributes

    // [SlashRequireAuth] - used instead of [SlashRequireOwner] to allow owners set in config.json
    // to use commands instead of restricting them to the bot account owner.
    public class SlashRequireAuthAttribute : SlashCheckBaseAttribute
    {
        public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
        {
            return Task.FromResult(Program.configjson.AuthorizedUsers.Contains(ctx.User.Id.ToString()));
        }
    }
}

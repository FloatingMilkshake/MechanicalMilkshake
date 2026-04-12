namespace MechanicalMilkshake;

internal class Program
{
    internal static async Task Main()
    {
        Setup.Constants.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MechanicalMilkshake (https://github.com/FloatingMilkshake/MechanicalMilkshake)");

        #region read config.json
        string json;
#if DEBUG
        const string configFile = "config.dev.json";
#else
        const string configFile = "config.json";
#endif
        await using (var fs = File.OpenRead(configFile))
        using (StreamReader sr = new(fs, new UTF8Encoding(false)))
        {
            json = await sr.ReadToEndAsync();
        }

        Setup.Configuration.ConfigJson = JsonConvert.DeserializeObject<ConfigJson>(json);

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.HomeChannel) ||
            string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.HomeServer) ||
            string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.BotToken))
        {
            Console.WriteLine("You are missing required values in your config.json file! Please ensure 'botToken', 'homeServer' and 'homeChannel' are set.");
            Environment.Exit(1);
        }

        if (string.IsNullOrEmpty(Setup.Configuration.ConfigJson.UptimeKumaHeartbeatUrl))
            Setup.State.Process.LastUptimeKumaHeartbeatStatus = "disabled";
        #endregion read config.json

        #region set up Discord client
        var clientBuilder = DiscordClientBuilder.CreateDefault(Setup.Configuration.ConfigJson.BotToken,
            DiscordIntents.All.RemoveIntent(DiscordIntents.GuildPresences).RemoveIntent(DiscordIntents.GuildMembers));
#if DEBUG
        clientBuilder.SetLogLevel(LogLevel.Debug);
#else
        clientBuilder.SetLogLevel(LogLevel.Information);
#endif
        clientBuilder.ConfigureServices(services =>
        {
            services.Replace<IGatewayController, GatewayController>();
        });
        clientBuilder.ConfigureExtraFeatures(config =>
        {
            config.LogUnknownEvents = false;
            config.LogUnknownAuditlogs = false;
        });
        clientBuilder.ConfigureEventHandlers(builder =>
            builder.HandleSessionCreated(ReadyEvent.HandleReadyEventAsync)
                    .HandleMessageCreated(MessageEvents.HandleMessageCreatedEventAsync)
                    .HandleMessageUpdated(MessageEvents.HandleMessageUpdatedEventAsync)
                    .HandleMessageDeleted(MessageEvents.HandleMessageDeletedEventAsync)
                    .HandleChannelDeleted(ChannelEvents.HandleChannelDeletedEventAsync)
                    .HandleComponentInteractionCreated(InteractionEvents.HandleComponentInteractionCreatedEventAsync)
                    .HandleModalSubmitted(InteractionEvents.HandleModalSubmittedEventAsync)
                    .HandleGuildCreated(GuildEvents.HandleGuildCreatedEventAsync)
                    .HandleGuildDeleted(GuildEvents.HandleGuildDeletedEventAsync)
                    .HandleGuildDownloadCompleted(GuildEvents.HandleGuildDownloadCompletedEventAsync)
        );
        clientBuilder.UseInteractivity(new InteractivityConfiguration
        {
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(30)
        });
        clientBuilder.UseCommands((_, extension) =>
        {
            // Use custom TextCommandProcessor to set custom prefixes & disable CommandNotFoundExceptions
            TextCommandProcessor textCommandProcessor = new(new()
            {
                PrefixResolver = new DefaultPrefixResolver(true, ["pls"]).ResolvePrefixAsync,
                EnableCommandNotFoundException = false
            });
            extension.AddProcessor(textCommandProcessor);

            // Use custom SlashCommandProcessor to use UnconditionallyOverwriteCommands
            SlashCommandProcessor slashCommandProcessor = new(new()
            {
                UnconditionallyOverwriteCommands = true,
            });
            extension.AddProcessor(slashCommandProcessor);

            // Register context checks
            extension.AddCheck<RequireBotCommanderCheck>();
            extension.AddCheck<ServerSpecificFeatures.CommandChecks.AllowedServersContextCheck>();

            // Register error handling
            extension.CommandErrored += Errors.CommandErrors.HandleCommandErroredEventAsync;

            // Register logging
            extension.CommandExecuted += Events.InteractionEvents.HandleCommandExecutedEventAsync;

            // Register interaction commands
            CommandHelpers.RegisterCommands(extension);

        }, new CommandsConfiguration
        {
            UseDefaultCommandErrorHandler = false,
        });

        Setup.State.Discord.Client = clientBuilder.Build();
        #endregion set up Discord client

        await SetupHelpers.CheckConfigurationAsync();

        await Setup.State.Discord.Client.ConnectAsync();

        // Give bot time to connect before starting tasks
        await Task.Delay(TimeSpan.FromSeconds(3));

        #region one-off tasks
        // Populate BotInformation.ApplicationCommands
        await Task.Run(async () => CommandTasks.ExecuteAsync());
        #endregion one-off tasks

        #region recurring tasks
        // Reminder check
        await Task.Run(async () => ReminderTasks.ExecuteAsync());

        // Redis connection check
        await Task.Run(async () => RedisTasks.ExecuteAsync());

        // DBots stats update
        await Task.Run(async () => DBotsTasks.ExecuteAsync());
        #endregion recurring tasks

        // Send startup message
        await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(await DebugInfoHelpers.GenerateDebugInfoEmbedAsync(true));

        // Wait indefinitely, let tasks continue running in async threads
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

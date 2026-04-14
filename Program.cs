namespace MechanicalMilkshake;

internal class Program
{
    internal static async Task Main()
    {
        Setup.Constants.HttpClient.DefaultRequestHeaders.UserAgent.ParseAdd("MechanicalMilkshake (https://github.com/FloatingMilkshake/MechanicalMilkshake)");

        #region read config.json
#if DEBUG
        const string configFile = "config.dev.json";
#else
        const string configFile = "config.json";
#endif
        Setup.Configuration.ConfigJson = JsonConvert.DeserializeObject<Setup.Types.ConfigJson>(await File.ReadAllTextAsync(configFile));

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
            DiscordIntents.AllUnprivileged.AddIntent(DiscordIntents.MessageContents));
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
            builder.HandleSessionCreated(ReadyEvents.HandleReadyEventAsync)
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
            extension.AddCheck<RequireBotCommanderContextCheck>();

            // Register error handling
            extension.CommandErrored += Errors.CommandErrors.HandleCommandErroredEventAsync;

            // Register logging
            extension.CommandExecuted += Events.InteractionEvents.HandleCommandExecutedEventAsync;

            // Register commands
            extension.RegisterCommands();

        }, new CommandsConfiguration
        {
            UseDefaultCommandErrorHandler = false,
        });

        Setup.State.Discord.Client = clientBuilder.Build();
        #endregion set up Discord client

        await CheckConfigurationAsync();

        await Setup.State.Discord.Client.ConnectAsync();

        // Give bot time to connect before starting tasks
        await Task.Delay(TimeSpan.FromSeconds(3));

        #region one-off tasks
        // Populate list of application commands
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
        await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(await Setup.Types.DebugInfo.CreateDebugInfoEmbedAsync(true));

        // Wait indefinitely, let tasks continue running in async threads
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }

    private static async Task CheckConfigurationAsync()
    {
        try
        {
            Setup.Configuration.Discord.HomeServer =
                await Setup.State.Discord.Client.GetGuildAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.HomeServer));
            Setup.Configuration.Discord.Channels.Home =
                await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.HomeChannel));
        }
        catch (Exception)
        {
            Setup.State.Discord.Client.Logger.LogCritical("\"homeChannel\" or \"homeServer\" in config.json are misconfigured. Please make sure you have a valid ID for both of these values.");
            Environment.Exit(1);
        }

        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.FeedbackChannel))
        {
            try
            {
                Setup.Configuration.Discord.Channels.Feedback =
                    await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.FeedbackChannel));
            }
            catch (Exception)
            {
                Setup.State.Discord.Client.Logger.LogWarning("Feedback command disabled due to invalid or missing channel ID.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.GuildLogChannel))
        {
            try
            {
                Setup.Configuration.Discord.Channels.GuildLogs =
                    await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.GuildLogChannel));
            }
            catch (Exception)
            {
                Setup.State.Discord.Client.Logger.LogWarning("Guild join/leave logs disabled due to invalid or missing channel ID.");
            }
        }

        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.SlashCommandLogChannel))
        {
            try
            {
                Setup.Configuration.Discord.Channels.CommandLogs =
                    await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.SlashCommandLogChannel));
            }
            catch (Exception)
            {
                Setup.State.Discord.Client.Logger.LogWarning("Interaction command logs disabled due to invalid or missing channel ID.");
            }
        }

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.WolframAlphaAppId))
        {
            Setup.State.Discord.Client.Logger.LogWarning("WolframAlpha commands disabled due to missing App ID.");
        }

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.UptimeKumaHeartbeatUrl))
        {
            Setup.State.Discord.Client.Logger.LogWarning("Uptime Kuma heartbeats disabled due to missing push URL.");
        }

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.DbotsApiToken))
        {
            Setup.State.Discord.Client.Logger.LogWarning("DBots stats posting disabled due to missing configuration.");
        }
    }
}

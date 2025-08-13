namespace MechanicalMilkshake;

public class GatewayController : IGatewayController
{
    public async Task HeartbeatedAsync(IGatewayClient client) => await HeartbeatEvent.Heartbeated(client);
    public async Task ResumeAttemptedAsync(IGatewayClient _) { }
    public async Task ZombiedAsync(IGatewayClient _) { }
    public async Task ReconnectRequestedAsync(IGatewayClient _) { }
    public async Task ReconnectFailedAsync(IGatewayClient _) { }
    public async Task SessionInvalidatedAsync(IGatewayClient _) { }
}

public class Program
{
    public static DiscordClient Discord;
    private static readonly string[] Prefixes = ["pls"];
    public static readonly List<string> DisabledCommands = [];
    public static readonly Random Random = new();
    public static DateTime ConnectTime;
    public static readonly HttpClient HttpClient = new();
    public static ConfigJson ConfigJson;
    public static readonly string ProcessStartTime = DateTime.Now.ToString(CultureInfo.CurrentCulture);
    public static readonly DiscordColor BotColor = new("#FAA61A");
    public static DiscordChannel HomeChannel;
    public static DiscordGuild HomeServer;
    public static List<DiscordApplicationCommand> ApplicationCommands;
    public static EventId BotEventId { get; } = new(1000, "MechanicalMilkshake");
#if DEBUG
    private static readonly ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("localhost:6379");
#else
    private static readonly ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("redis");
#endif
    public static readonly IDatabase Db = Redis.GetDatabase();
    public static bool RedisExceptionsSuppressed;
    public static readonly Entities.MessageCaching.MessageCache MessageCache = new();
    public static string LastUptimeKumaHeartbeatStatus = "N/A";
    public static bool GuildDownloadCompleted = false;

    public static readonly Dictionary<string, ulong> UserFlagEmoji = new()
    {
        { "earlyVerifiedBotDeveloper", 1000168738970144779 },
        { "discordStaff", 1000168738022228088 },
        { "hypesquadBalance", 1000168740073242756 },
        { "hypesquadBravery", 1000168740991811704 },
        { "hypesquadBrilliance", 1000168741973266462 },
        { "hypesquadEvents", 1000168742535303312 },
        { "bugHunterLevelOne", 1000168734666793001 },
        { "bugHunterLevelTwo", 1000168735740526732 },
        { "certifiedModerator", 1000168736789118976 },
        { "partneredServerOwner", 1000168744192053298 },
        { "verifiedBot1", 1000229381563744397 },
        { "verifiedBot2", 1000229382431977582 },
        { "earlySupporter", 1001317583124971582 }
    };

    internal static async Task Main()
    {
        // Read config.json, or config.dev.json if running in development mode
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

        ConfigJson = JsonConvert.DeserializeObject<ConfigJson>(json);

        if (string.IsNullOrWhiteSpace(ConfigJson.HomeChannel) ||
            string.IsNullOrWhiteSpace(ConfigJson.HomeServer) ||
            string.IsNullOrWhiteSpace(ConfigJson.BotToken))
        {
            Discord.Logger.LogError(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "You are missing required values in your config.json file! Exiting...");
            Environment.Exit(1);
        }
        
        var clientBuilder = DiscordClientBuilder.CreateDefault(ConfigJson.BotToken, DiscordIntents.All.RemoveIntent(DiscordIntents.GuildPresences));
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
            builder.HandleSessionCreated(ReadyEvent.OnReady)
                    .HandleMessageCreated(MessageEvents.MessageCreated)
                    .HandleMessageUpdated(MessageEvents.MessageUpdated)
                    .HandleMessageDeleted(MessageEvents.MessageDeleted)
                    .HandleChannelDeleted(ChannelEvents.ChannelDeleted)
                    .HandleComponentInteractionCreated(ComponentInteractionEvent.ComponentInteractionCreated)
                    .HandleGuildCreated(GuildEvents.GuildCreated)
                    .HandleGuildDeleted(GuildEvents.GuildDeleted)
                    .HandleGuildDownloadCompleted(GuildEvents.GuildDownloadCompleted)
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
                PrefixResolver = new DefaultPrefixResolver(true, Prefixes).ResolvePrefixAsync,
                EnableCommandNotFoundException = false
            });
            extension.AddProcessor(textCommandProcessor);
            
            // Register context checks
            extension.AddCheck<RequireAuthCheck>();
            extension.AddCheck<ServerSpecificFeatures.CommandChecks.AllowedServersContextCheck>();
            
            // Register error handling
            extension.CommandErrored += ErrorEvents.CommandErrored;
            
            // Register logging
            extension.CommandExecuted += Events.InteractionEvents.CommandExecuted;
            
            // Register interaction commands
            CommandHelpers.RegisterCommands(extension, HomeServer.Id);
            
        }, new CommandsConfiguration
        {
            UseDefaultCommandErrorHandler = false
        });
        
        // Build the client
        Discord = clientBuilder.Build();

        // Set home channel & guild for later reference
        ulong homeChanId = default;
        ulong homeServerId = default;
        try
        {
            homeChanId = Convert.ToUInt64(ConfigJson.HomeChannel);
            homeServerId = Convert.ToUInt64(ConfigJson.HomeServer);
        }
        catch
        {
            Discord.Logger.LogError(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "\"homeChannel\" or \"homeServer\" in config.json are misconfigured. Please make sure you have a valid ID for both of these values.");
            Environment.Exit(1);
        }

        HomeChannel = await Discord.GetChannelAsync(homeChanId);
        HomeServer = await Discord.GetGuildAsync(homeServerId);

        if (ConfigJson is null || ConfigJson.WolframAlphaAppId == "")
        {
            Discord.Logger.LogWarning(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "WolframAlpha commands disabled due to missing App ID.");

            DisabledCommands.Add("wa");
        }

        if (ConfigJson is null || ConfigJson.FeedbackChannel == "")
        {
            Discord.Logger.LogWarning(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "Feedback command disabled due to missing channel ID.");

            DisabledCommands.Add("feedback");
        }
        
        if (ConfigJson.UptimeKumaHeartbeatUrl is null or "")
        {
            Discord.Logger.LogWarning(BotEventId, "Uptime Kuma heartbeats disabled due to missing push URL.");
        }

        await Discord.ConnectAsync();

        // Run tasks

        // Delay to give bot time to connect
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Start tasks
        
        // Populate ApplicationCommands
        await Task.Run(async () => CommandTasks.ExecuteAsync());
        
        // Reminder check
        await Task.Run(async () => ReminderTasks.ExecuteAsync());
        
        // Database connection check
        await Task.Run(async () => DatabaseTasks.ExecuteAsync());
        
        // Custom status update
        await Task.Run(async () => ActivityTasks.ExecuteAsync());
        
        // DBots stats update
        await Task.Run(async () => DBotsTasks.ExecuteAsync());
        
        // Send startup message
        await Program.HomeChannel.SendMessageAsync(await DebugInfoHelpers.GenerateDebugInfoEmbed(true));

        // Wait indefinitely, let tasks continue running in async threads
        await Task.Delay(System.Threading.Timeout.InfiniteTimeSpan);
    }
}

// Set custom command attributes

// [RequireAuth] - used instead of [RequireOwner] or [SlashRequireOwner] to allow owners set in config.json
// to use commands instead of restricting them to the bot account owner.
public class RequireAuthAttribute : ContextCheckAttribute;

public class RequireAuthCheck : IContextCheck<RequireAuthAttribute>
{
#nullable enable
    public ValueTask<string?> ExecuteCheckAsync(RequireAuthAttribute _, CommandContext ctx) =>
        ValueTask.FromResult(Program.ConfigJson.BotCommanders.Contains(ctx.User.Id.ToString())
            ? null
            : "The user is not authorized to use this command.");
}

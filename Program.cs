using System.Threading;

namespace MechanicalMilkshake;

public class Program
{
    public static DiscordClient Discord;
    private static readonly string[] Prefixes = ["pls"];
    public static MinioClient Minio;
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
    public static readonly MessageCache MessageCache = new();

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

        if (ConfigJson?.Base is null)
        {
            Discord.Logger.LogCritical(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "Your config.json file is malformed. Please be sure it has all of the required values.");
            Environment.Exit(1);
        }

        if (string.IsNullOrWhiteSpace(ConfigJson.Base.HomeChannel) ||
            string.IsNullOrWhiteSpace(ConfigJson.Base.HomeServer) ||
            string.IsNullOrWhiteSpace(ConfigJson.Base.BotToken))
        {
            Discord.Logger.LogError(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "You are missing required values in your config.json file. Please make sure you have values for all of the keys under \"base\".");
            Environment.Exit(1);
        }

        // Configure Discord client and interactivity
        Discord = new DiscordClient(new DiscordConfiguration
        {
            Token = ConfigJson.Base.BotToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.AllUnprivileged | DiscordIntents.GuildMessages | DiscordIntents.GuildMembers
                | DiscordIntents.MessageContents | DiscordIntents.GuildPresences,
            LogUnknownEvents = false,
#if DEBUG
            MinimumLogLevel = LogLevel.Debug
#else
            MinimumLogLevel = LogLevel.Information
#endif
        });
        Discord.UseInteractivity(new InteractivityConfiguration
        {
            PollBehaviour = PollBehaviour.KeepEmojis,
            Timeout = TimeSpan.FromSeconds(30)
        });

        // Set home channel & guild for later reference
        ulong homeChanId = default;
        ulong homeServerId = default;
        try
        {
            homeChanId = Convert.ToUInt64(ConfigJson.Base.HomeChannel);
            homeServerId = Convert.ToUInt64(ConfigJson.Base.HomeServer);
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

        // Set up slash commands and CommandsNext
        var slash = Discord.UseSlashCommands();

        var commands = Discord.UseCommandsNext(new CommandsNextConfiguration
        {
            StringPrefixes = Prefixes,
            EnableDefaultHelp = false
        });

        // Set up Minio (used for some Owner commands)
        if (ConfigJson.S3.Bucket == "" || ConfigJson.S3.CdnBaseUrl == "" || ConfigJson.S3.Endpoint == "" ||
            ConfigJson.S3.AccessKey == "" || ConfigJson.S3.SecretKey == "" || ConfigJson.S3.Region == "" ||
            ConfigJson.Cloudflare.UrlPrefix == "" || ConfigJson.Cloudflare.ZoneId == "" ||
            ConfigJson.Cloudflare.Token == "")
        {
            Discord.Logger.LogWarning(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "CDN commands disabled due to missing S3 or Cloudflare information.");

            DisabledCommands.Add("cdn");
        }
        else
        {
            Minio = new MinioClient()
                .WithEndpoint(ConfigJson.S3.Endpoint)
                .WithCredentials(ConfigJson.S3.AccessKey, ConfigJson.S3.SecretKey)
                .WithRegion(ConfigJson.S3.Region)
                .WithSSL();
        }

        if (ConfigJson.WorkerLinks.BaseUrl == "" || ConfigJson.WorkerLinks.Secret == "" ||
            ConfigJson.WorkerLinks.NamespaceId == "" || ConfigJson.WorkerLinks.ApiKey == "" ||
            ConfigJson.WorkerLinks.AccountId == "" || ConfigJson.WorkerLinks.Email == "")
        {
            Discord.Logger.LogWarning(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "Short-link commands disabled due to missing WorkerLinks information.");

            DisabledCommands.Add("wl");
        }

        if (ConfigJson.Base.WolframAlphaAppId == "")
        {
            Discord.Logger.LogWarning(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "WolframAlpha commands disabled due to missing App ID.");

            DisabledCommands.Add("wa");
        }

        if (ConfigJson.Ids.FeedbackChannel == "")
        {
            Discord.Logger.LogWarning(BotEventId,
                // ReSharper disable once LogMessageIsSentenceProblem
                "Feedback command disabled due to missing channel ID.");

            DisabledCommands.Add("feedback");
        }
        
        if (ConfigJson.Base.UptimeKumaHeartbeatUrl is null or "")
        {
            Discord.Logger.LogWarning(BotEventId, "Uptime Kuma heartbeats disabled due to missing push URL.");
        }

        // Register slash commands as guild commands in home server when
        // running in development mode
#if DEBUG
        var slashCommandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace is not null && t.Namespace.Contains("MechanicalMilkshake.Commands") &&
            !t.IsNested);

        foreach (var type in slashCommandClasses)
            slash.RegisterCommands(type, HomeServer.Id);
        
        if (ConfigJson.Base.UseServerSpecificFeatures)
        {
            // Register CommandsNext commands
            commands.RegisterCommands<ServerSpecificFeatures.MessageCommands>();
            
            // Register server-specific feature slash commands in home server when debugging
            slash.RegisterCommands<ServerSpecificFeatures.RoleCommands>(HomeServer.Id);
        }

        Discord.Logger.LogInformation(BotEventId, "Slash commands registered for debugging");

        // Register slash commands globally for 'production' bot
#else
        var globalSlashCommandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace is not null && t.Namespace.Contains("MechanicalMilkshake.Commands") &&
            !t.Namespace.Contains("MechanicalMilkshake.Commands.Owner.HomeServerCommands") && !t.IsNested);

        foreach (var type in globalSlashCommandClasses)
            slash.RegisterCommands(type);


        var ownerSlashCommandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace is not null &&
            t.Namespace.Contains("MechanicalMilkshake.Commands.Owner.HomeServerCommands") &&
            !t.IsNested);

        foreach (var type in ownerSlashCommandClasses)
        {
            slash.RegisterCommands(type, HomeServer.Id);
            slash.RegisterCommands(type, 1007457740655968327);
        }

        // Register server-specific feature slash commands
        slash.RegisterCommands<ServerSpecificFeatures.RoleCommands>(984903591816990730);
        slash.RegisterCommands<ServerSpecificFeatures.RoleCommands>(HomeServer.Id);

        Discord.Logger.LogInformation(BotEventId, "Slash commands registered globally");
        
        // and CommandsNext commands
        commands.RegisterCommands<ServerSpecificFeatures.MessageCommands>();
#endif

        await Discord.ConnectAsync();

        // Store registered application commands for later reference
#if DEBUG
        ApplicationCommands =
            (List<DiscordApplicationCommand>)await Discord.GetGuildApplicationCommandsAsync(
                HomeServer.Id);
#else
        ApplicationCommands = (List<DiscordApplicationCommand>)await Discord.GetGlobalApplicationCommandsAsync();
#endif

        // Events
        Discord.Ready += ReadyEvent.OnReady;
        Discord.MessageCreated += MessageEvents.MessageCreated;
        Discord.MessageUpdated += MessageEvents.MessageUpdated;
        Discord.MessageDeleted += MessageEvents.MessageDeleted;
        Discord.ChannelDeleted += ChannelEvents.ChannelDeleted;
        Discord.ComponentInteractionCreated += ComponentInteractionEvent.ComponentInteractionCreated;
        Discord.GuildCreated += GuildEvents.GuildCreated;
        Discord.GuildDeleted += GuildEvents.GuildDeleted;
        Discord.GuildMemberUpdated += GuildEvents.GuildMemberUpdated;
        Discord.Heartbeated += HeartbeatEvent.Heartbeated;
        commands.CommandErrored += ErrorEvents.CommandsNextService_CommandErrored;
        slash.SlashCommandErrored += ErrorEvents.SlashCommandErrored;
        slash.SlashCommandExecuted += InteractionEvents.SlashCommandExecuted;
        slash.ContextMenuExecuted += InteractionEvents.ContextMenuExecuted;

        /* Fix SSH key permissions at bot startup.
        I wanted to be able to do this somewhere else, but for now it seems
        like this is the best way of doing it that I'm aware of, and it works. */
#if !DEBUG
        await EvalCommands.RunCommand("cat /app/id_ed25519 > ~/.ssh/id_ed25519 && chmod 700 ~/.ssh/id_ed25519");
#endif

        // Run tasks

        // Delay to give bot time to connect
        await Task.Delay(TimeSpan.FromSeconds(1));
        
        // Start checks
        
        // Package update check
        await Task.Run(async () => PackageUpdateTasks.ExecuteAsync());
        
        // Reminder check
        await Task.Run(async () => ReminderTasks.ExecuteAsync());
        
        // Database connection check
        await Task.Run(async () => DatabaseTasks.ExecuteAsync());
        
        // Custom status update
        await Task.Run(async () => ActivityTasks.ExecuteAsync());
        
        // DBots stats update
        await Task.Run(async () => DBotsTasks.ExecuteAsync());

        // Wait indefinitely, let tasks continue running in async threads
        await Task.Delay(Timeout.InfiniteTimeSpan);
    }
}

// Set custom command attributes

// [SlashRequireAuth] - used instead of [SlashRequireOwner] to allow owners set in config.json
// to use commands instead of restricting them to the bot account owner.
public class SlashRequireAuthAttribute : SlashCheckBaseAttribute
{
    public override Task<bool> ExecuteChecksAsync(InteractionContext ctx)
    {
        return Task.FromResult(Program.ConfigJson.Base.AuthorizedUsers.Contains(ctx.User.Id.ToString()));
    }
}

// Message command version of [SlashRequireAuth]
public class RequireAuthAttribute : CheckBaseAttribute
{
    public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
    {
        return Task.FromResult(Program.ConfigJson.Base.AuthorizedUsers.Contains(ctx.User.Id.ToString()));
    }
}

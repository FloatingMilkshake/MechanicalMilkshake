using System.Globalization;
using MechanicalMilkshake.Checks;

namespace MechanicalMilkshake;

internal class Program
{
    public static DiscordClient Discord;
    public static MinioClient Minio;
    public static readonly Random Random = new();
    public static DateTime ConnectTime;
    public static readonly HttpClient HttpClient = new();
    public static ConfigJson ConfigJson;
    public static readonly string ProcessStartTime = DateTime.Now.ToString(CultureInfo.CurrentCulture);
    public static readonly DiscordColor BotColor = new("#FAA61A");
    public static DiscordChannel HomeChannel;
    public static List<DiscordApplicationCommand> ApplicationCommands;
    public static EventId BotEventId { get; } = new(1000, "MechanicalMilkshake");
#if DEBUG
    private static readonly ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("localhost:6379");
#else
    private static readonly ConnectionMultiplexer Redis = ConnectionMultiplexer.Connect("redis");
#endif
    public static readonly IDatabase Db = Redis.GetDatabase();

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

    // Use a custom help message to show that there are no CommandsNext commands since they
    // have all been migrated to slash commands
    private class CustomHelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder _embed;

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
            _embed.Description =
                $"No commands are available with this prefix.\n\nAll commands have been moved to slash (`/`) commands. Type `/` and select {Discord.CurrentUser.Username} to see available commands.";
            _embed.Color = DiscordColor.Red;

            return this;
        }

        public override CommandHelpMessage Build()
        {
            return new CommandHelpMessage(embed: _embed);
        }
    }

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
                "Your config.json file is malformed. Please be sure it has all of the required values.");
            Environment.Exit(1);
        }

        // If the bot token is not set, we cannot continue. Display error and exit.
        if (string.IsNullOrWhiteSpace(ConfigJson.Base.BotToken))
        {
            Discord.Logger.LogError(BotEventId,
                "ERROR: No token provided! Make sure the botToken field in your config.json file is set.");
            Environment.Exit(1);
        }

        // Configure Discord client and interactivity
        Discord = new DiscordClient(new DiscordConfiguration
        {
            Token = ConfigJson.Base.BotToken,
            TokenType = TokenType.Bot,
            Intents = DiscordIntents.All,
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

        // Get home channel ID; if the home channel is not set, we cannot continue.
        // Display error and exit.
        if (ConfigJson.Base.HomeChannel == null)
        {
            Discord.Logger.LogError(BotEventId,
                "No home channel provided! Make sure the homeChannel field in your config.json file is set.");
            Environment.Exit(1);
        }

        var homeChannelId = Convert.ToUInt64(ConfigJson.Base.HomeChannel);
        HomeChannel = await Discord.GetChannelAsync(homeChannelId);

        // Set up slash commands and CommandsNext
        var slash = Discord.UseSlashCommands();

        var commands = Discord.UseCommandsNext(new CommandsNextConfiguration());

        // Use the custom help message set with CustomHelpFormatter
        commands.SetHelpFormatter<CustomHelpFormatter>();

        // Set up Minio (used for some Owner commands)
        Minio = new MinioClient()
            .WithEndpoint(ConfigJson.S3.Endpoint)
            .WithCredentials(ConfigJson.S3.AccessKey, ConfigJson.S3.SecretKey)
            .WithRegion(ConfigJson.S3.Region)
            .WithSSL();


        // Register slash commands as guild commands in home server when
        // running in development mode
#if DEBUG
        var slashCommandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace != null && t.Namespace.Contains("MechanicalMilkshake.Commands") &&
            !t.IsNested);

        foreach (var type in slashCommandClasses)
            slash.RegisterCommands(type, ConfigJson.Base.HomeServerId);

        slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(ConfigJson.Base.HomeServerId);

        Discord.Logger.LogInformation(BotEventId, "Slash commands registered for debugging.");

        // Register slash commands globally for 'production' bot
#else
        var globalSlashCommandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace != null && t.Namespace.Contains("MechanicalMilkshake.Commands") &&
            !t.Namespace.Contains("MechanicalMilkshake.Commands.Owner.HomeServerCommands") && !t.IsNested);

        foreach (var type in globalSlashCommandClasses)
            slash.RegisterCommands(type);


        var ownerSlashCommandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace != null &&
            t.Namespace.Contains("MechanicalMilkshake.Commands.Owner.HomeServerCommands") &&
            !t.IsNested);

        foreach (var type in ownerSlashCommandClasses)
            slash.RegisterCommands(type, ConfigJson.Base.HomeServerId);

        Discord.Logger.LogInformation(BotEventId, "Slash commands registered globally.");

// Register slash commands for per-server features in respective servers
// & testing server for 'production' bot
        slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(631118217384951808);
        slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(984903591816990730);
        slash.RegisterCommands<PerServerFeatures.ComplaintSlashCommands>(ConfigJson.Base.HomeServerId);
        slash.RegisterCommands<PerServerFeatures.RoleCommands>(984903591816990730);
#endif

        // Register CommandsNext commands
        commands.RegisterCommands<PerServerFeatures.MessageCommands>();

        await Discord.ConnectAsync();

        // Store registered application commands for later reference
#if DEBUG
        ApplicationCommands =
            (List<DiscordApplicationCommand>)await Discord.GetGuildApplicationCommandsAsync(
                ConfigJson.Base.HomeServerId);
#else
        ApplicationCommands = (List<DiscordApplicationCommand>)await Discord.GetGlobalApplicationCommandsAsync();
#endif

        // Events
        Discord.Ready += ReadyEvent.OnReady;
        Discord.MessageCreated += MessageEvents.MessageCreated;
        Discord.MessageUpdated += MessageEvents.MessageUpdated;
        Discord.ComponentInteractionCreated += ComponentInteractionEvent.ComponentInteractionCreated;
        Discord.GuildCreated += GuildEvents.GuildCreated;
        Discord.GuildDeleted += GuildEvents.GuildDeleted;
        commands.CommandErrored += ErrorEvents.CommandsNextService_CommandErrored;
        slash.SlashCommandErrored += ErrorEvents.SlashCommandErrored;

        /* Create an instance of the Owner.Private class and run a command to fix SSH key permissions
        at bot startup. I wanted to be able to do this somewhere else, but for now it seems
        like this is the best way of doing it that I'm aware of, and it works. */
        await EvalCommands.RunCommand("cat /app/id_rsa > ~/.ssh/id_rsa && chmod 700 ~/.ssh/id_rsa");

        // Run checks

        // Delay to give bot time to connect
        await Task.Delay(1000);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () =>
        {
            while (true)
            {
                await PackageUpdateChecks.PackageUpdateCheck();
                await Task.Delay(21600000); // 6 hours
            }
            // ReSharper disable once FunctionNeverReturns
        });

        Task.Run(async () =>
        {
            var returnValue = true;
            while (returnValue)
            {
                returnValue = await ReminderChecks.ReminderCheck();
                await Task.Delay(10000); // 10 seconds
            }
        });

        while (true)
        {
            await Task.Delay(3600000); // 1 hour
            await CustomStatusHelper.SetCustomStatus();
        }
        // ReSharper disable once FunctionNeverReturns
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
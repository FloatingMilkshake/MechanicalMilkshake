namespace MechanicalMilkshake.Setup;

internal class State
{
    internal class Process
    {
        internal static bool RedisExceptionsSuppressed;
        internal static readonly DateTime ProcessStartTime = DateTime.Now;
        internal static string LastUptimeKumaHeartbeatStatus = "waiting";
    }

    internal class Discord
    {
        internal static DiscordClient Client;
        internal static DateTime ConnectTime;
        internal static bool GuildDownloadCompleted = false;
    }

    internal class Commands
    {
        internal static readonly List<DiscordApplicationCommand> ApplicationCommands = [];
    }

    internal class Caches
    {
        internal static readonly Types.MessageCaching.MessageCache MessageCache = new();
        // <user ID, reminder>; used to pass context between reminder modify interactions
        internal static Dictionary<ulong, Setup.Types.Reminder> ReminderModifyCache = new();
        // <user ID, message from context>; used to pass context to modal handling (from "Remind Me About This" ctx menu cmd)
        internal static Dictionary<ulong, DiscordMessage> ReminderInteractionCache = new();
        // <confirmation message ID, messages to clear>; used to apss context between /clear and confirmation button
        internal static readonly Dictionary<ulong, List<DiscordMessage>> ClearCache = new();
        // <message ID, CancellationToken>; used to pass CancellationTokens around
        public static readonly Dictionary<ulong, CancellationTokenSource> CancellationTokens = new();
    }
}

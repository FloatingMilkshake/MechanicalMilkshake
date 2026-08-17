namespace MechanicalMilkshake.Setup.State;

public static class Caches
{
    public static readonly Types.MessageCache MessageCache = new();

    // <user ID, reminder>; used to pass context between reminder modify interactions
    internal static Dictionary<ulong, Setup.Types.Reminder> ReminderModifyCache = [];

    // <user ID, message from context>; used to pass context to modal handling
    internal static Dictionary<ulong, DiscordMessage> ReminderInteractionCache = [];

    // <confirmation message ID, messages to clear>; used to apss context between /clear and confirmation button
    internal static readonly Dictionary<ulong, List<DiscordMessage>> ClearCache = [];

    // <message ID, CancellationToken>; used to pass CancellationTokens around
    public static readonly Dictionary<ulong, CancellationTokenSource> CancellationTokens = [];
}

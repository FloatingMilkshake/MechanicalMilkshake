namespace MechanicalMilkshake.Setup.State;

internal static class Discord
{
    internal static DiscordClient Client;
    internal static DateTime ConnectTime;
    internal static bool GuildDownloadCompleted = false;
    internal static readonly List<DiscordApplicationCommand> ApplicationCommands = [];
    internal static readonly List<DiscordEmoji> ApplicationEmoji = [];
    internal static DiscordGuild HomeServer;

    internal static class Channels
    {
        internal static DiscordChannel Home;
        internal static DiscordChannel GuildLogs;
        internal static DiscordChannel CommandLogs;
        internal static DiscordChannel Feedback;
    }
}

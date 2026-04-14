namespace MechanicalMilkshake.Setup;

internal static class Configuration
{
    internal static Setup.Types.ConfigJson ConfigJson;

    internal static class Discord
    {
        internal static DiscordGuild HomeServer;

        internal static class Channels
        {
            internal static DiscordChannel Home;
            internal static DiscordChannel GuildLogs;
            internal static DiscordChannel CommandLogs;
            internal static DiscordChannel Feedback;
        }
    }
}

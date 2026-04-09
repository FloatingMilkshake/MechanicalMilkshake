namespace MechanicalMilkshake.Setup;

internal class Configuration
{
    internal static ConfigJson ConfigJson;

    internal class Discord
    {
        internal static DiscordGuild HomeServer;

        internal class Channels
        {
            internal static DiscordChannel Home;
            internal static DiscordChannel GuildLogs;
            internal static DiscordChannel CommandLogs;
            internal static DiscordChannel Feedback;
        }
    }
}

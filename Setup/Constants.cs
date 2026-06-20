namespace MechanicalMilkshake.Setup;

internal static class Constants
{
    internal static readonly DiscordColor BotColor = new("#FAA61A");
    internal static readonly HttpClient HttpClient = new();
    internal static readonly List<ulong> UserFlagEmoji =
    [
#if DEBUG
        1494525865013415956, // bugHunterLevelOne
        1494525866393206795, // bugHunterLevelTwo
        1494525868381442220, // certifiedModerator
        1494525870197444648, // discordStaff
        1494525871652868106, // earlySupporter
        1494525873049571440, // earlyVerifiedBotDeveloper
        1494525874488217720, // hypesquadBalance
        1494525876426113054, // hypesquadBravery
        1494525877680078959, // hypesquadBrilliance
        1494525878774923344, // hypesquadEvents
        1494525883489190041, // partneredServerOwner
        1494525884357672991, // verifiedBot1
        1494525886093856788  // verifiedBot2
#else
        1494525865013415956, // bugHunterLevelOne
        1494525866393206795, // bugHunterLevelTwo
        1494525868381442220, // certifiedModerator
        1494525870197444648, // discordStaff
        1494525871652868106, // earlySupporter
        1494525873049571440, // earlyVerifiedBotDeveloper
        1494525874488217720, // hypesquadBalance
        1494525876426113054, // hypesquadBravery
        1494525877680078959, // hypesquadBrilliance
        1494525878774923344, // hypesquadEvents
        1494525883489190041, // partneredServerOwner
        1494525884357672991, // verifiedBot1
        1494525886093856788  // verifiedBot2
#endif
    ];
    internal static class RegularExpressions
    {
#pragma warning disable SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
        internal static readonly Regex DiscordUrlPattern = new(@".*discord(?:app)?.com\/channels\/((?:@)?[a-z0-9]*)\/([0-9]*)(?:\/)?([0-9]*)");
        internal static readonly Regex UrlPattern = new(@"(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]");
        internal static readonly Regex EmojiPattern = new("<(a)?:([A-Za-z0-9_]*):([0-9]*)>");
        internal static readonly Regex DiscordInvitePattern = new(@"discord(?:app)?\.(?:gg|com\/invite)\/([\w+-]+)");
        internal static readonly Regex DiscordIdPattern = new("[0-9]{15,19}");
        internal static readonly Regex ReminderIdPattern = new("[0-9]{4}");
#pragma warning restore SYSLIB1045 // Convert to 'GeneratedRegexAttribute'.
    }
}

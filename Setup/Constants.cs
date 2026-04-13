namespace MechanicalMilkshake.Setup;

internal static class Constants
{
    internal static readonly DiscordColor BotColor = new("#FAA61A");
    internal static readonly HttpClient HttpClient = new();
    internal static readonly Dictionary<string, ulong> UserFlagEmoji = new()
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

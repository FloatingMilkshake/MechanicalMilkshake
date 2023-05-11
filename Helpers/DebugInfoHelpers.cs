namespace MechanicalMilkshake.Helpers;

public class DebugInfoHelpers
{
    public static DebugInfo GetDebugInfo()
    {
        var commitHash = FileHelpers.ReadFile("CommitHash.txt", "dev");

        var commitTime = FileHelpers.ReadFile("CommitTime.txt");
        string commitTimeDescription;

        if (commitTime == "")
        {
            var unixTime = ((DateTimeOffset)Program.ConnectTime).ToUnixTimeSeconds();
            commitTime = $"<t:{unixTime}:F>";
            commitTimeDescription = "Last connected to Discord at";
        }
        else
        {
            var unixTime = ((DateTimeOffset)Convert.ToDateTime(commitTime)).ToUnixTimeSeconds();
            commitTime = $"<t:{unixTime}:F>";

            commitTimeDescription = "Commit Timestamp";
        }

        var commitMessage = FileHelpers.ReadFile("CommitMessage.txt",
            $"Running in development mode; process started at {Program.ProcessStartTime}");

        var loadTime = (Program.ConnectTime - Convert.ToDateTime(Program.ProcessStartTime)).Humanize();

        return new DebugInfo(
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            $"DSharpPlus {Program.Discord.VersionString}",
            loadTime,
            commitHash,
            commitTimeDescription,
            commitTime,
            commitMessage
        );
    }

    // If provided a DebugInfo object, use that...
    private static Task<DiscordEmbed> GenerateDebugInfoEmbed(DebugInfo debugInfo, bool isOnReadyEvent)
    {
        DiscordEmbedBuilder embed = new()
        {
            Title = isOnReadyEvent ? "Connected!" : "Debug Info",
            Color = Program.BotColor
        };

        var remoteUrl = FileHelpers.ReadFile("RemoteUrl.txt");
        var commitHash = debugInfo.CommitHash == "dev"
            ? "`dev`"
            : $"[`{debugInfo.CommitHash}`]({remoteUrl}/commit/{debugInfo.CommitHash})";

        embed.AddField("Framework", debugInfo.Framework, true);
        embed.AddField("Platform", debugInfo.Platform, true);
        embed.AddField("Library", debugInfo.Library, true);
        embed.AddField("Server Count", Program.Discord.Guilds.Count.ToString(), true);
        embed.AddField("Command Count", Program.ApplicationCommands.Count.ToString(), true);
        if (isOnReadyEvent) embed.AddField("Load Time", debugInfo.LoadTime, true);
        embed.AddField("Commit Hash", commitHash, true);
        embed.AddField(debugInfo.CommitTimeDescription, debugInfo.CommitTimestamp, true);
        embed.AddField("Commit Message", debugInfo.CommitMessage);

        return Task.FromResult<DiscordEmbed>(embed);
    }

    // ...otherwise, get debug info manually
    public static async Task<DiscordEmbed> GenerateDebugInfoEmbed(bool isOnReadyEvent)
    {
        return await GenerateDebugInfoEmbed(GetDebugInfo(), isOnReadyEvent);
    }


    public class DebugInfo
    {
        public DebugInfo(string framework = null, string platform = null, string library = null,
            string loadTime = null, string commitHash = null, string commitTimeDescription = null,
            string commitTimestamp = null, string commitMessage = null)
        {
            Framework = framework;
            Platform = platform;
            Library = library;
            LoadTime = loadTime;
            CommitHash = commitHash;
            CommitTimeDescription = commitTimeDescription;
            CommitTimestamp = commitTimestamp;
            CommitMessage = commitMessage;
        }

        public string Framework { get; }
        public string Platform { get; }
        public string Library { get; }
        public string LoadTime { get; }
        public string CommitHash { get; }
        public string CommitTimeDescription { get; }
        public string CommitTimestamp { get; }
        public string CommitMessage { get; }
    }
}
namespace MechanicalMilkshake.Helpers;

public class DebugInfoHelpers
{
    private static DebugInfo GetDebugInfo()
    {
        var commitHash = "";
        if (File.Exists("CommitHash.txt"))
        {
            StreamReader readHash = new("CommitHash.txt");
            commitHash = readHash.ReadToEnd().Trim();
        }

        if (commitHash == "") commitHash = "dev";

        var commitTime = "";
        var commitTimeDescription = "";
        if (File.Exists("CommitTime.txt"))
        {
            StreamReader readTime = new("CommitTime.txt");
            commitTime = readTime.ReadToEnd();

            var dateToConvert = Convert.ToDateTime(commitTime);
            var unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();
            commitTime = $"<t:{unixTime}:F>";

            commitTimeDescription = "Commit Timestamp";
        }

        if (commitTime == "")
        {
            var unixTime = ((DateTimeOffset)Program.ConnectTime).ToUnixTimeSeconds();
            commitTime = $"<t:{unixTime}:F>";
            commitTimeDescription = "Last connected to Discord at";
        }

        var commitMessage = "";
        if (File.Exists("CommitMessage.txt"))
        {
            StreamReader readMessage = new("CommitMessage.txt");
            commitMessage = readMessage.ReadToEnd();
        }

        if (commitMessage == "")
            commitMessage = $"Running in development mode; process started at {Program.ProcessStartTime}";

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
    private static async Task<DiscordEmbed> GenerateDebugInfoEmbed(DebugInfo debugInfo, bool isOnReadyEvent)
    {
        DiscordEmbedBuilder embed = new()
        {
            Title = isOnReadyEvent ? "Connected!" : "Debug Info",
            Color = Program.BotColor
        };

        embed.AddField("Framework", debugInfo.Framework, true);
        embed.AddField("Platform", debugInfo.Platform, true);
        embed.AddField("Library", debugInfo.Library, true);
        embed.AddField("Server Count", Program.Discord.Guilds.Count.ToString(), true);
        embed.AddField("Command Count", Program.ApplicationCommands.Count.ToString(), true);
        embed.AddField("Load Time", debugInfo.LoadTime, true);
        embed.AddField("Commit Hash", $"`{debugInfo.CommitHash}`", true);
        embed.AddField(debugInfo.CommitTimeDescription, debugInfo.CommitTimestamp, true);
        embed.AddField("Commit Message", debugInfo.CommitMessage);

        return embed;
    }

    // ...otherwise, get debug info manually
    public static async Task<DiscordEmbed> GenerateDebugInfoEmbed(bool isOnReadyEvent)
    {
        return await GenerateDebugInfoEmbed(GetDebugInfo(), isOnReadyEvent);
    }


    private class DebugInfo
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
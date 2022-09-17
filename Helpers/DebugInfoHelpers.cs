namespace MechanicalMilkshake.Helpers;

public class DebugInfoHelpers
{
    public static DebugInfo GetDebugInfo()
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
            var unixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();
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
            commitMessage = $"Running in development mode; process started at {Program.processStartTime}";

        var loadTime = (Program.connectTime - Convert.ToDateTime(Program.processStartTime)).Humanize();

        return new DebugInfo(
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            $"DSharpPlus {Program.discord.VersionString}",
            loadTime,
            commitHash,
            commitTimeDescription,
            commitTime,
            commitMessage
        );
    }

    // If provided a DebugInfo object, use that...
    public static async Task<DiscordEmbed> GenerateDebugInfoEmbed(DebugInfo debugInfo, bool isOnReadyEvent)
    {
        DiscordEmbedBuilder embed = new()
        {
            Title = isOnReadyEvent ? "Connected!" : "Debug Info",
            Color = Program.botColor
        };

        embed.AddField("Framework", debugInfo.Framework, true);
        embed.AddField("Platform", debugInfo.Platform, true);
        embed.AddField("Library", debugInfo.Library, true);
        embed.AddField("Server Count", Program.discord.Guilds.Count.ToString(), true);

        int commandCount;
#if DEBUG
        commandCount = (await Program.discord.GetGuildApplicationCommandsAsync(Program.configjson.Base.HomeServerId))
            .Count;
#else
        commandCount = (await Program.discord.GetGlobalApplicationCommandsAsync()).Count;
#endif
        embed.AddField("Command Count", commandCount.ToString(), true);
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

        public string Framework { get; set; }
        public string Platform { get; set; }
        public string Library { get; set; }
        public string LoadTime { get; set; }
        public string CommitHash { get; set; }
        public string CommitTimeDescription { get; set; }
        public string CommitTimestamp { get; set; }
        public string CommitMessage { get; set; }
    }
}
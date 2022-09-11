namespace MechanicalMilkshake.Helpers;

public class DebugInfoHelper
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
            framework: RuntimeInformation.FrameworkDescription,
            platform: RuntimeInformation.OSDescription,
            library: $"DSharpPlus {Program.discord.VersionString}",
            loadTime: loadTime,
            commitHash: commitHash,
            commitTimeDescription: commitTimeDescription,
            commitTimestamp: commitTime,
            commitMessage: commitMessage
            );
    }

    public class DebugInfo
    {
        public string Framework { get; set; }
        public string Platform { get; set; }
        public string Library { get; set; }
        public string LoadTime { get; set; }
        public string CommitHash { get; set; }
        public string CommitTimeDescription { get; set; }
        public string CommitTimestamp { get; set; }
        public string CommitMessage { get; set; }

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
    }
}
using System.Threading;

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

        while (Program.ConnectTime == default)
            Thread.Sleep(100);
        var timeSinceProcessStart = (Program.ConnectTime - Convert.ToDateTime(Program.ProcessStartTime)).Humanize();

        return new DebugInfo(
            RuntimeInformation.FrameworkDescription,
            RuntimeInformation.OSDescription,
            $"DSharpPlus {Program.Discord.VersionString}",
            timeSinceProcessStart,
            commitHash,
            commitTimeDescription,
            commitTime,
            commitMessage
        );
    }

    // If provided a DebugInfo object, use that...
    private static Task<DiscordEmbed> GenerateDebugInfoEmbed(DebugInfo debugInfo, bool isOnStartup)
    {
        // Check whether GuildDownloadCompleted has been fired yet
        // If not, wait until it has
        while (!Program.GuildDownloadCompleted)
            Task.Delay(1000).Wait();
        
        DiscordEmbedBuilder embed = new()
        {
            Title = isOnStartup ? "Connected!" : "",
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
        if (isOnStartup) embed.AddField("Time Since Process Start", debugInfo.TimeSinceProcessStart, true);
        embed.AddField("Commit Hash", commitHash, true);
        embed.AddField(debugInfo.CommitTimeDescription, debugInfo.CommitTimestamp, true);
        embed.AddField("Commit Message", debugInfo.CommitMessage);

        return Task.FromResult<DiscordEmbed>(embed);
    }

    // ...otherwise, get debug info manually
    public static async Task<DiscordEmbed> GenerateDebugInfoEmbed(bool isOnStartup)
    {
        return await GenerateDebugInfoEmbed(GetDebugInfo(), isOnStartup);
    }


    public class DebugInfo(
        string framework = null,
        string platform = null,
        string library = null,
        string timeSinceProcessStart = null,
        string commitHash = null,
        string commitTimeDescription = null,
        string commitTimestamp = null,
        string commitMessage = null)
    {
        public string Framework { get; } = framework;
        public string Platform { get; } = platform;
        public string Library { get; } = library;
        public string TimeSinceProcessStart { get; } = timeSinceProcessStart;
        public string CommitHash { get; } = commitHash;
        public string CommitTimeDescription { get; } = commitTimeDescription;
        public string CommitTimestamp { get; } = commitTimestamp;
        public string CommitMessage { get; } = commitMessage;
    }
}
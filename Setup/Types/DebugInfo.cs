namespace MechanicalMilkshake.Setup.Types;

internal sealed class DebugInfo
{
    internal string DotNetVersion { get; private set; }
    internal string OperatingSystem { get; private set; }
    internal string DSharpPlusVersion { get; private set; }
    internal string CommitInformation { get; private set; }

    private DebugInfo(
        string dotnetVersion,
        string operatingSystem,
        string DSharpPlusVersion,
        string commitInformation)
    {
        DotNetVersion = dotnetVersion;
        OperatingSystem = operatingSystem;
        this.DSharpPlusVersion = DSharpPlusVersion;
        CommitInformation = commitInformation;
    }

    internal static async Task<DebugInfo> GetDebugInfoAsync()
    {
        return new DebugInfo(
            dotnetVersion: RuntimeInformation.FrameworkDescription.Replace(".NET", "").Trim(),
            operatingSystem: RuntimeInformation.OSDescription,
            DSharpPlusVersion: Setup.State.Discord.Client.VersionString.Split('+').First(),
            commitInformation: await GetCommitInformationAsync()
        );
    }

    private static async Task<string> GetCommitInformationAsync()
    {
#if DEBUG
        return "dev";
#else
            var commitHash = await File.ReadAllTextOrFallbackAsync("CommitHash.txt");
            var commitTimestamp = $"<t:{Convert.ToDateTime(await File.ReadAllTextOrFallbackAsync("CommitTime.txt")).ToUnixTimeSeconds()}:F>";
            var commitMessage = await File.ReadAllTextOrFallbackAsync("CommitMessage.txt");
            var remoteUrl = await File.ReadAllTextOrFallbackAsync("RemoteUrl.txt");

            return $"[`{commitHash}`]({remoteUrl}/commit/{commitHash}): {commitMessage}\n{commitTimestamp}";
#endif
    }

    internal static async Task<DiscordEmbedBuilder> CreateDebugInfoEmbedAsync(bool isOnStartup)
    {
        // Check whether GuildDownloadCompleted has been fired yet
        // If not, wait until it has
        while (!Setup.State.Discord.GuildDownloadCompleted)
            await Task.Delay(1000);

        var debugInfo = await GetDebugInfoAsync();

        return new DiscordEmbedBuilder()
        {
            Title = isOnStartup ? "Connected!" : null,
            Color = Setup.Constants.BotColor
        }
        .AddField(".NET Version", debugInfo.DotNetVersion, true)
        .AddField("Operating System", debugInfo.OperatingSystem, true)
        .AddField("DSharpPlus Version", debugInfo.DSharpPlusVersion, true)
        .AddField("Version", debugInfo.CommitInformation, false);
    }
}

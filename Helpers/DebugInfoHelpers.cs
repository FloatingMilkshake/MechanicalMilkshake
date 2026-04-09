namespace MechanicalMilkshake.Helpers;

internal class DebugInfoHelpers
{
    internal static async Task<DiscordEmbedBuilder> GenerateDebugInfoEmbedAsync(bool isOnStartup)
    {
        // Check whether GuildDownloadCompleted has been fired yet
        // If not, wait until it has
        while (!Setup.State.Discord.GuildDownloadCompleted)
            await Task.Delay(1000);

        var debugInfo = await Setup.Types.DebugInfo.GetDebugInfoAsync();

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

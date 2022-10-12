namespace MechanicalMilkshake.Checks;

public class PackageUpdateChecks
{
    public static async Task PackageUpdateCheck()
    {
        var updatesAvailableResponse = "";
        var restartRequiredResponse = "";

        EvalCommands evalCommands = new();
        var updatesAvailable = false;
        var restartRequired = false;
        foreach (var host in Program.configjson.Base.SshHosts)
        {
#if DEBUG
            Program.discord.Logger.LogInformation(Program.BotEventId,
                "[PackageUpdateCheck] Checking for updates on host '{host}'.\"", host);
#endif
            var cmdResult =
                await evalCommands.RunCommand($"ssh {host} \"cat /var/run/reboot-required ; sudo apt update\"");
            if (cmdResult.Contains(" can be upgraded"))
            {
                updatesAvailableResponse += $"`{host}`\n";
                updatesAvailable = true;
            }

            if (cmdResult.Contains("System restart required")) restartRequired = true;
        }
#if DEBUG
        Program.discord.Logger.LogInformation(Program.BotEventId,
            "[PackageUpdateCheck] Finished checking for updates on all hosts.");
#endif

        if (restartRequired) restartRequiredResponse = "A system restart is required to complete package updates.";

        if (updatesAvailable || restartRequired)
        {
            if (updatesAvailable)
                updatesAvailableResponse = "Package updates are available on the following hosts:\n" +
                                           updatesAvailableResponse;

            var ownerMention = "";
            foreach (var user in Program.discord.CurrentApplication.Owners) ownerMention += user.Mention + " ";

            var response = updatesAvailableResponse + restartRequiredResponse;
            await Program.homeChannel.SendMessageAsync($"{ownerMention.Trim()}\n{response}");
        }
    }
}
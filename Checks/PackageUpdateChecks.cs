namespace MechanicalMilkshake.Checks;

public class PackageUpdateChecks
{
    public static async Task<(int numhostsChecked, int totalNumHosts)> PackageUpdateCheck()
    {
        var numHostsChecked = 0;
        var totalNumHosts = Program.ConfigJson.Base.SshHosts.Length;
        if (totalNumHosts == 0) return (0, 0);
        
        var updatesAvailableResponse = "";
        var restartRequiredResponse = "";

        var updatesAvailable = false;
        var restartRequired = false;

        foreach (var host in Program.ConfigJson.Base.SshHosts)
        {
            Program.Discord.Logger.LogDebug(Program.BotEventId,
                "[PackageUpdateCheck] Checking for updates on host '{Host}'.\"", host);

            var cmdResult =
                await EvalCommands.RunCommand($"ssh {host} \"cat /var/run/reboot-required ; sudo apt update\"", false);

            if (string.IsNullOrWhiteSpace(cmdResult)) continue;

            if (cmdResult.Contains(" can be upgraded"))
            {
                updatesAvailableResponse += $"`{host}`\n";
                updatesAvailable = true;
            }

            if (cmdResult.Contains("System restart required")) restartRequired = true;

            numHostsChecked++;
        }

        Program.Discord.Logger.LogDebug(Program.BotEventId,
            $"[PackageUpdateCheck] Finished checking for updates on {numHostsChecked}/{totalNumHosts} hosts");

        if (restartRequired) restartRequiredResponse = "A system restart is required to complete package updates.";

        if (updatesAvailable || restartRequired)
        {
            if (updatesAvailable)
                updatesAvailableResponse = "Package updates are available on the following hosts:\n" +
                                           updatesAvailableResponse;

            var ownerMention =
                Program.Discord.CurrentApplication.Owners.Aggregate("",
                    (current, user) => current + user.Mention + " ");

            var response = updatesAvailableResponse + restartRequiredResponse;
            await Program.HomeChannel.SendMessageAsync($"{ownerMention.Trim()}\n{response}");
        }

        return (numHostsChecked, totalNumHosts);
    }
}
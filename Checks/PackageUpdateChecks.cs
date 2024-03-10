namespace MechanicalMilkshake.Checks;

public class PackageUpdateChecks
{
    public static async Task<(int numhostsChecked, int totalNumHosts, string checkResult)>
        PackageUpdateCheck(bool isPeriodicCheck)
    {
        var numHostsChecked = 0;
        var totalNumHosts = Program.ConfigJson.Base.SshHosts.Length;
        if (totalNumHosts == 0) return (0, 0, "");
        
        var updatesAvailableResponse = "";

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
                // Get hostname of machine
                Regex hostnamePattern = new(@"[A-Za-z0-9-]+\@([A-Za-z0-9-]+)");
                var hostnameMatch = hostnamePattern.Match(host);
                var hostname = hostnameMatch.Groups[1].Value;
                
                // Get number of packages that can be upgraded
                Regex numPackageUpdatesPattern = new(@"([0-9]+) packages can be upgraded");
                var numPackageUpdatesMatch = numPackageUpdatesPattern.Match(cmdResult);
                var numPackageUpdates = numPackageUpdatesMatch.Groups[1].Value;

                updatesAvailableResponse +=
                    $"{hostname}: {numPackageUpdates} package{(int.Parse(numPackageUpdates) > 1 ? "s" : "")}";
                updatesAvailable = true;
            }

            if (cmdResult.Contains("System restart required"))
            {
                updatesAvailableResponse += "*";
                restartRequired = true;
            }

            updatesAvailableResponse += "\n";

            numHostsChecked++;
        }

        Program.Discord.Logger.LogDebug(Program.BotEventId,
            $"[PackageUpdateCheck] Finished checking for updates on {numHostsChecked}/{totalNumHosts} hosts");

        if (updatesAvailable || restartRequired)
        {
            string restartRequiredMessage = "";
            if (restartRequired)
                restartRequiredMessage = " Hosts marked with a * require a restart to apply updates.";
            
            if (updatesAvailable)
                updatesAvailableResponse = $"Package updates are available.{restartRequiredMessage}\n{updatesAvailableResponse}";

            var ownerMention =
                Program.Discord.CurrentApplication.Owners.Aggregate("",
                    (current, user) => current + user.Mention + " ");

            var response = updatesAvailableResponse;
            if (isPeriodicCheck) await Program.HomeChannel.SendMessageAsync($"{ownerMention.Trim()}\n{response}");
        }

        return (numHostsChecked, totalNumHosts, updatesAvailableResponse);
    }
}
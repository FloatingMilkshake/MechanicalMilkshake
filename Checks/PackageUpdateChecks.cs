using Microsoft.VisualBasic.CompilerServices;

namespace MechanicalMilkshake.Checks;

public class PackageUpdateChecks
{
    public static async Task<(int numhostsChecked, int totalNumHosts, string checkResult)> PackageUpdateCheck()
    {
        var numHostsChecked = 0;
        var totalNumHosts = Program.ConfigJson.Base.SshHosts.Length;
        if (totalNumHosts == 0) return (0, 0, "");

        List<UpdateCheckResult> updateCheckResults = [];
        
        var updatesAvailableResponse = "";

        var updatesAvailable = false;
        var restartRequired = false;
        var numPackageUpdates = 0;

        foreach (var rawHost in Program.ConfigJson.Base.SshHosts)
        {
            // Get port from host if provided; otherwise, use SSH default of 22
            var host = rawHost;
            var port = "22";
            if (rawHost.Contains(':'))
            {
                host = rawHost.Split(':')[0];
                port = rawHost.Split(':')[1];
            }
            
            Program.Discord.Logger.LogDebug(Program.BotEventId,
                "[PackageUpdateCheck] Checking for updates on host '{Host}'.", host);

            var cmdResult =
                await EvalCommands.RunCommand($"ssh {host} -p {port} \"cat /var/run/reboot-required ; sudo apt update\"");

            Program.Discord.Logger.LogDebug(Program.BotEventId,
                "[PackageUpdateCheck] Finished checking for updates on host '{Host}' with code {ExitCode}.",
                host, cmdResult.ExitCode);
            if (!string.IsNullOrWhiteSpace(cmdResult.Output))
                Program.Discord.Logger.LogDebug(Program.BotEventId,
                    "[PackageUpdateCheck] Output:\n{Output}", cmdResult.Output);
            if (!string.IsNullOrWhiteSpace(cmdResult.Error))
                Program.Discord.Logger.LogDebug(Program.BotEventId,
                    "[PackageUpdateCheck] Error:\n{Error}", cmdResult.Error);

            if (string.IsNullOrWhiteSpace(cmdResult.Output)) continue;
            
            // Get hostname of machine
            Regex hostnamePattern = new(@"[A-Za-z0-9-]+\@([A-Za-z0-9-]+)");
            var hostnameMatch = hostnamePattern.Match(host);
            var hostname = hostnameMatch.Groups[1].Value;

            if (cmdResult.Output.Contains(" can be upgraded"))
            {
                // Get number of packages that can be upgraded
                Regex numPackageUpdatesPattern = new(@"([0-9]+) package[s]? can be upgraded");
                var numPackageUpdatesMatch = numPackageUpdatesPattern.Match(cmdResult.Output);
                var numPackageUpdatesStr = numPackageUpdatesMatch.Groups[1].Value;
                numPackageUpdates = numPackageUpdatesStr == "" ? 0 : int.Parse(numPackageUpdatesStr);
                updatesAvailable = true;
            }

            if (cmdResult.Output.Contains("System restart required"))
                restartRequired = true;
            
            // assemble result & add to list
            updateCheckResults.Add(new UpdateCheckResult(hostname, numPackageUpdates, restartRequired));

            numHostsChecked++;
        }

        Program.Discord.Logger.LogDebug(Program.BotEventId,
            $"[PackageUpdateCheck] Finished checking for updates on {numHostsChecked}/{totalNumHosts} hosts");

        if (updatesAvailable || restartRequired)
        {
            var restartRequiredMessage = restartRequired
                ? "A restart is required on some hosts (\\*) to apply updates."
                : "";
            
            var updatesAvailableMessage = updatesAvailable
                ? "Package updates are available."
                : "";

            var hostsWithUpdates = "";
            foreach (var result in updateCheckResults)
            {
                if (result.UpdateCount > 0 || result.RestartRequired)
                {
                    hostsWithUpdates += $"{result.Hostname}{(result.RestartRequired ? "\\*" : "")}";
                    
                    if (result.UpdateCount > 0)
                        hostsWithUpdates += $": {result.UpdateCount} update{(result.UpdateCount > 1 ? "s" : "")}";
                }
            }
            
            updatesAvailableResponse = $"{updatesAvailableMessage} {restartRequiredMessage}\n{hostsWithUpdates}";
        }

        return (numHostsChecked, totalNumHosts, updatesAvailableResponse);
    }
}
namespace MechanicalMilkshake.Tasks;

internal class CommandTasks
{
    internal static async Task ExecuteAsync()
    {
        await PopulateApplicationCommandListAsync();
        await RunApplicationCommandRegistrationWatchdogAsync();
    }

    private static async Task PopulateApplicationCommandListAsync()
    {
        var applicationCommands = await Setup.State.Discord.Client.GetGlobalApplicationCommandsAsync() as List<DiscordApplicationCommand> ?? [];
        applicationCommands.AddRange(await Setup.State.Discord.Client.GetGuildApplicationCommandsAsync(Setup.Configuration.Discord.HomeServer.Id) as List<DiscordApplicationCommand> ?? []);
        applicationCommands = applicationCommands.Distinct().ToList();

        Setup.State.Discord.ApplicationCommands.AddRange(applicationCommands);
    }

    private static async Task RunApplicationCommandRegistrationWatchdogAsync()
    {
        // Wait 5 minutes for potential ratelimits etc
        // Registration should happen well before this, this is a bit generous
        await Task.Delay(300000);

        if (Setup.State.Discord.ApplicationCommands.Count == 0)
        {
            if (!File.Exists("/proc/self/cgroup"))
            {
                await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(string.Join(" ", Setup.State.Discord.Client.CurrentApplication.Owners.Select(o => o.Mention))
                    + " Application commands have not been successfully registered. The bot is not running under Docker and cannot restart on its own."
                    + " Please restart the bot to attempt registration again.");
                return;
            }

            await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(string.Join(" ", Setup.State.Discord.Client.CurrentApplication.Owners.Select(o => o.Mention))
                    + " Application commands have not been successfully registered. Restarting to attempt registration again...");
            Environment.Exit(1);
        }
    }
}

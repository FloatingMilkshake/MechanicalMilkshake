namespace MechanicalMilkshake.Tasks;

internal class CommandTasks
{
    internal static async Task ExecuteAsync()
    {
        while (true)
        {
            await PopulateApplicationCommandListAsync();
            return;
        }
    }

    private static async Task PopulateApplicationCommandListAsync()
    {
        var applicationCommands = await Setup.State.Discord.Client.GetGlobalApplicationCommandsAsync() as List<DiscordApplicationCommand> ?? [];
        applicationCommands.AddRange(await Setup.State.Discord.Client.GetGuildApplicationCommandsAsync(Setup.Configuration.Discord.HomeServer.Id) as List<DiscordApplicationCommand> ?? []);
        applicationCommands = applicationCommands.Distinct().ToList();

        Setup.State.Commands.ApplicationCommands.AddRange(applicationCommands);
    }
}

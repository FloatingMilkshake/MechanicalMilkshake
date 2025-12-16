namespace MechanicalMilkshake.Tasks;

public class CommandTasks
{
    public static async Task ExecuteAsync()
    {
        while (true)
        {
            await PopulateApplicationCommandListAsync();
            await Task.Delay(TimeSpan.FromMinutes(1));
        }
    }
    
    public static async Task PopulateApplicationCommandListAsync()
    {
        var applicationCommands = await Program.Discord.GetGlobalApplicationCommandsAsync() as List<DiscordApplicationCommand> ?? [];
        applicationCommands.AddRange(await Program.Discord.GetGuildApplicationCommandsAsync(Program.HomeServer.Id) as List<DiscordApplicationCommand> ?? []);
        applicationCommands = applicationCommands.Distinct().ToList();
        
        Program.ApplicationCommands = applicationCommands;
    }
}
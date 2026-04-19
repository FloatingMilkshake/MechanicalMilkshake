namespace MechanicalMilkshake.Extensions;

internal static class CommandsExtensionExtensions
{
    extension(CommandsExtension commandsExtension)
    {
        internal void RegisterCommands()
        {
            var commandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
                t.IsClass && !t.IsNested && t.Namespace is not null && t.Namespace == "MechanicalMilkshake.Commands" &&
                t != typeof(Commands.DebugCommands)).ToList();

            commandsExtension.AddCommands(commandClasses);
        
            RegisterPrivateCommands(commandsExtension);
        }

        private void RegisterPrivateCommands()
        {
            commandsExtension.AddCommands(typeof(Commands.DebugCommands), Setup.Configuration.Discord.HomeServer.Id);

            if (Setup.Configuration.ConfigJson.UseServerSpecificFeatures)
            {
                // Commands specified in server-specific features
                // Register in home server when debugging, or home server + respective servers otherwise
                commandsExtension.AddCommands(typeof(ServerSpecificFeatures.Commands.MessageCommands), Setup.Configuration.Discord.HomeServer.Id);
            }
        }
    }
}

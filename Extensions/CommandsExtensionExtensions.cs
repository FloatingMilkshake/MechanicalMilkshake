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

            commandsExtension.RecursiveAddCommands(commandClasses);

            RegisterPrivateCommands(commandsExtension);
        }

        private void RecursiveAddCommands(List<Type> commandClasses)
        {
            foreach (var commandClass in commandClasses)
            {
                commandsExtension.AddCommands(commandClass);
                foreach (var nestedClass in commandClass.GetNestedTypes())
                {
                    commandsExtension.AddCommands(nestedClass);
                }
            }
        }

        private void RegisterPrivateCommands()
        {
            commandsExtension.AddCommands(typeof(Commands.DebugCommands), Setup.State.Discord.HomeServer.Id);

            if (Setup.State.Process.Configuration.UseServerSpecificFeatures)
            {
                // Commands specified in server-specific features
                // Register in home server when debugging, or home server + respective servers otherwise
                commandsExtension.AddCommands(typeof(ServerSpecificFeatures.Commands.MessageCommands), Setup.State.Discord.HomeServer.Id);
            }
        }
    }
}

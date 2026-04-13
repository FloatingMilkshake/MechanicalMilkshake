namespace MechanicalMilkshake.Extensions;

internal static class CommandsExtensionExtensions
{
    extension(CommandsExtension commandsExtension)
    {
        internal void RegisterCommands()
        {
            var commandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
                t.IsClass && !t.IsNested && t.Namespace is not null && t.Namespace == "MechanicalMilkshake.Commands").ToList();

            RegisterUserInstallCommands(commandsExtension, commandClasses);
            RegisterPublicGuildInstallCommands(commandsExtension, commandClasses);
            RegisterHomeServerCommands(commandsExtension, commandClasses);
            RegisterPrivateCommands(commandsExtension);
        }

        private void RegisterUserInstallCommands(List<Type> commandClasses)
        {
            // Always register globally

            List<Type> commandClassesToRegister = [];
            List<MethodInfo> commandMethodsToRegister = [];

            foreach (var commandClass in commandClasses)
            {
                if (commandClass.GetCustomAttributes().Any(a => a is InteractionInstallTypeAttribute ita &&
                    ita.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall)))
                {
                    commandClassesToRegister.Add(commandClass);
                    continue;
                }

                foreach (var commandMethod in commandClass.GetMethods())
                {
                    if (commandMethod.GetCustomAttributes().Any(a => a is InteractionInstallTypeAttribute ita &&
                        ita.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall)))
                    {
                        commandMethodsToRegister.Add(commandMethod);
                    }
                }
            }

            foreach (var commandClass in commandClassesToRegister)
            {
                commandsExtension.AddCommands(commandClass);
                foreach (var nestedType in commandClass.GetNestedTypes())
                {
                    commandsExtension.AddCommands(nestedType);
                }
            }

            foreach (var commandMethod in commandMethodsToRegister)
            {
                commandsExtension.AddCommand(CreateDelegateForCommand(commandMethod));
            }
        }

        private void RegisterPublicGuildInstallCommands(List<Type> commandClasses)
        {
            // Register in home server when debugging, globally otherwise

            List<Type> commandClassesToRegister = [];
            List<MethodInfo> commandMethodsToRegister = [];

            foreach (var commandClass in commandClasses)
            {
                var commandClassCustomAttributes = commandClass.GetCustomAttributes();

                if (commandClassCustomAttributes.Any(a => a is RequireHomeServerAttribute))
                    continue;

                if (commandClassCustomAttributes.Any(a => a is InteractionInstallTypeAttribute ita &&
                    ita.InstallTypes.Contains(DiscordApplicationIntegrationType.GuildInstall) &&
                    !ita.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall)))
                {
                    commandClassesToRegister.Add(commandClass);
                    continue;
                }

                foreach (var commandMethod in commandClass.GetMethods())
                {
                    if (commandMethod.GetCustomAttributes().Any(a => a is InteractionInstallTypeAttribute ita &&
                        ita.InstallTypes.Contains(DiscordApplicationIntegrationType.GuildInstall) &&
                        !ita.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall)))
                    {
                        commandMethodsToRegister.Add(commandMethod);
                    }
                }
            }

            foreach (var commandClass in commandClassesToRegister)
            {
#if DEBUG
                commandsExtension.AddCommands(commandClass, Setup.Configuration.Discord.HomeServer.Id);
#else
                commandsExtension.AddCommands(commandClass, Setup.Configuration.Discord.HomeServer.Id);
#endif
                foreach (var nestedType in commandClass.GetNestedTypes())
                {
#if DEBUG
                    commandsExtension.AddCommands(nestedType, Setup.Configuration.Discord.HomeServer.Id);
#else
                    commandsExtension.AddCommands(nestedType);
#endif
                }
            }

            foreach (var commandMethod in commandMethodsToRegister)
            {
#if DEBUG
                commandsExtension.AddCommand(CreateDelegateForCommand(commandMethod), Setup.Configuration.Discord.HomeServer.Id);
#else
                commandsExtension.AddCommand(CreateDelegateForCommand(commandMethod));
#endif
            }
        }

        private void RegisterHomeServerCommands(List<Type> commandClasses)
        {
            // [RequireHomeServer]
            // Register in home server always

            List<Type> commandClassesToRegister = [];
            List<MethodInfo> commandMethodsToRegister = [];

            foreach (var commandClass in commandClasses)
            {
                var commandClassCustomAttributes = commandClass.GetCustomAttributes();

                if (commandClassCustomAttributes.Any(a => a is RequireHomeServerAttribute))
                {
                    commandClassesToRegister.Add(commandClass);
                    continue;
                }

                foreach (var commandMethod in commandClass.GetMethods())
                {
                    if (commandMethod.GetCustomAttributes().Any(a => a is RequireHomeServerAttribute))
                    {
                        commandMethodsToRegister.Add(commandMethod);
                    }
                }
            }

            foreach (var commandClass in commandClassesToRegister)
            {
                commandsExtension.AddCommands(commandClass, Setup.Configuration.Discord.HomeServer.Id);
                foreach (var nestedType in commandClass.GetNestedTypes())
                {
                    commandsExtension.AddCommands(nestedType, Setup.Configuration.Discord.HomeServer.Id);
                }
            }

            foreach (var commandMethod in commandMethodsToRegister)
            {
                commandsExtension.AddCommand(CreateDelegateForCommand(commandMethod), Setup.Configuration.Discord.HomeServer.Id);
            }
        }

        private void RegisterPrivateCommands()
        {
            if (!Setup.Configuration.ConfigJson.UseServerSpecificFeatures)
                return;

            // Commands specified in server-specific features
            // Register in home server when debugging, or home server + respective servers otherwise
            commandsExtension.AddCommands(typeof(ServerSpecificFeatures.Commands.MessageCommands), Setup.Configuration.Discord.HomeServer.Id);
        }
    }

    private static Delegate CreateDelegateForCommand(MethodInfo commandMethod)
    {
        var parameterTypes = commandMethod.GetParameters().Select(p => p.ParameterType).Concat([commandMethod.ReturnType]).ToArray();

        Type delegateType = System.Linq.Expressions.Expression.GetDelegateType(parameterTypes);

        return commandMethod.CreateDelegate(delegateType);
    }
}

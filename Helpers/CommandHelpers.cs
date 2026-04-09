namespace MechanicalMilkshake.Helpers;

internal class CommandHelpers
{
    internal static string GetSlashCmdMention(string commandFullName)
    {
        if (char.IsUpper(commandFullName[0]) && !commandFullName.Contains(' '))
            // This is probably a context menu command.
            // Return it in inline code instead of a command mention.
            return $"`{commandFullName}`";

        if (Setup.State.Commands.ApplicationCommands is null)
            return $"`{string.Join(' ', commandFullName)}`";

        var command = Setup.State.Commands.ApplicationCommands.FirstOrDefault(c => c.Name == commandFullName.Split(' ').First());

        if (command is null)
            return $"`/{string.Join(' ', commandFullName)}`";

        return $"</{string.Join(' ', commandFullName)}:{command.Id}>";
    }

    internal static void RegisterCommands(CommandsExtension extension)
    {
        var commandClasses = Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && !t.IsNested && t.Namespace is not null && t.Namespace == "MechanicalMilkshake.Commands").ToList();

        RegisterUserInstallCommands(extension, commandClasses);
        RegisterPublicGuildInstallCommands(extension, commandClasses);
        RegisterHomeServerCommands(extension, commandClasses);
        RegisterPrivateCommands(extension);
    }

    private static void RegisterUserInstallCommands(CommandsExtension extension, List<Type> commandClasses)
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
            extension.AddCommands(commandClass);
            foreach (var nestedType in commandClass.GetNestedTypes())
            {
                extension.AddCommands(nestedType);
            }
        }

        foreach (var commandMethod in commandMethodsToRegister)
        {
            extension.AddCommand(CreateDelegateForCommand(commandMethod));
        }
    }

    private static void RegisterPublicGuildInstallCommands(CommandsExtension extension, List<Type> commandClasses)
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
            extension.AddCommands(commandClass, Setup.Configuration.Discord.HomeServer.Id);
#else
            extension.AddCommands(commandClass, Setup.Configuration.Discord.HomeServer.Id);
#endif
            foreach (var nestedType in commandClass.GetNestedTypes())
            {
#if DEBUG
                extension.AddCommands(nestedType, Setup.Configuration.Discord.HomeServer.Id);
#else
                extension.AddCommands(nestedType);
#endif
            }
        }

        foreach (var commandMethod in commandMethodsToRegister)
        {
#if DEBUG
            extension.AddCommand(CreateDelegateForCommand(commandMethod), Setup.Configuration.Discord.HomeServer.Id);
#else
            extension.AddCommand(CreateDelegateForCommand(commandMethod));
#endif
        }
    }

    private static void RegisterHomeServerCommands(CommandsExtension extension, List<Type> commandClasses)
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
            extension.AddCommands(commandClass, Setup.Configuration.Discord.HomeServer.Id);
            foreach (var nestedType in commandClass.GetNestedTypes())
            {
                extension.AddCommands(nestedType, Setup.Configuration.Discord.HomeServer.Id);
            }
        }

        foreach (var commandMethod in commandMethodsToRegister)
        {
            extension.AddCommand(CreateDelegateForCommand(commandMethod), Setup.Configuration.Discord.HomeServer.Id);
        }
    }

    private static void RegisterPrivateCommands(CommandsExtension extension)
    {
        // Commands specified in server-specific features
        // Register in home server when debugging, or home server + respective servers otherwise
#if DEBUG
        extension.AddCommands<ServerSpecificFeatures.Commands.RoleCommands>(Setup.Configuration.Discord.HomeServer.Id);
        extension.AddCommands<ServerSpecificFeatures.Commands.MessageCommands>(Setup.Configuration.Discord.HomeServer.Id);
#else
        extension.AddCommands<ServerSpecificFeatures.Commands.RoleCommands>(Setup.Configuration.Discord.HomeServer.Id, 984903591816990730);
        extension.AddCommands<ServerSpecificFeatures.Commands.MessageCommands>(Setup.Configuration.Discord.HomeServer.Id, 1203128266559328286);
#endif
    }

    private static Delegate CreateDelegateForCommand(MethodInfo commandMethod)
    {
        var parameterTypes = commandMethod.GetParameters().Select(p => p.ParameterType).Concat([commandMethod.ReturnType]).ToArray();

        Type delegateType = System.Linq.Expressions.Expression.GetDelegateType(parameterTypes);

        return commandMethod.CreateDelegate(delegateType);
    }
}

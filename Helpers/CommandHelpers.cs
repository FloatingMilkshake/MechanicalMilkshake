namespace MechanicalMilkshake.Helpers;

public class CommandHelpers
{
    /// <summary>
    ///     Responds to an interaction with a failure message if the command is disabled.
    ///     Commands are disabled if required configuration information is missing.
    /// </summary>
    /// <param name="ctx">Interaction context used to respond to the interaction.</param>
    /// <param name="isFollowUp">Whether to follow-up to the interaction (as opposed to creating a new interaction response).</param>
    public static async Task FailOnMissingInfo(MechanicalMilkshake.SlashCommandContext ctx, bool isFollowUp)
    {
        const string failureMsg =
            "This command is disabled! Please make sure you have provided values for all of the necessary keys in the config file.";

        if (isFollowUp)
            await ctx.FollowupAsync(failureMsg);
        else
            await ctx.RespondAsync(failureMsg);
    }
    
    /// <summary>
    ///     Registers commands with the provided CommandsExtension.
    /// </summary>
    /// <param name="extension">The extension to register commands with.</param>
    /// <param name="homeServerId">The ID of the server to register home server-only commands in.</param>
    public static void RegisterCommands(CommandsExtension extension, ulong homeServerId)
    {
        var publicInteractionCommandTypes = GetPublicInteractionCommandTypes();
        var userInstallCommandTypes = GetUserInstallInteractionCommandTypes(publicInteractionCommandTypes);
        var guildInstallCommandTypes = GetGuildInstallInteractionCommandTypes(publicInteractionCommandTypes);
        var homeServerSlashCommandTypes = GetHomeServerInteractionCommandTypes();
        
        // Always register owner commands in home server only (+ second private server)
        extension.AddCommands(homeServerSlashCommandTypes, homeServerId, 1342179809618559026);
        
        // Always register user-install commands globally
        extension.AddCommands(userInstallCommandTypes);
#if DEBUG
        // Register guild-install commands in home server when debugging
        extension.AddCommands(guildInstallCommandTypes, homeServerId);
        
        if (Program.ConfigJson.Base.UseServerSpecificFeatures)
        {
            // Register server-specific feature commands in home server when debugging
            extension.AddCommands<ServerSpecificFeatures.Commands.RoleCommands>(homeServerId);
            extension.AddCommands<ServerSpecificFeatures.Commands.MessageCommands>(homeServerId);
        }
#else
        // Register guild-install commands globally for 'production' bot
        extension.AddCommands(guildInstallCommandTypes);

        if (Program.ConfigJson.Base.UseServerSpecificFeatures)
        {
            // Register server-specific feature commands in respective guilds for 'production' bot
            extension.AddCommands<ServerSpecificFeatures.Commands.RoleCommands>(homeServerId, 984903591816990730);
            extension.AddCommands<ServerSpecificFeatures.Commands.MessageCommands>(homeServerId, 1203128266559328286);
        }
#endif
    }
    
    /// <summary>
    ///     Gets a list of all public interaction command types. To be used for registering commands.
    /// </summary>
    /// <returns>A list of public interaction command types.</returns>
    private static List<Type> GetPublicInteractionCommandTypes()
    {
        return Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace is not null && t.Namespace.Contains("MechanicalMilkshake.Commands") &&
            !t.IsNested).ToList();
    }
    
    /// <summary>
    ///     Gets a list of all interaction command types that allow user-install. To be used for registering commands.
    /// </summary>
    /// <param name="publicInteractionCommandTypes">A list of all public interaction command types to filter through.</param>
    /// <returns>A list of all interaction command types that allow user-install.</returns>
    private static List<Type> GetUserInstallInteractionCommandTypes(List<Type> publicInteractionCommandTypes)
    {
        List<Type> userInstallInteractionCommandTypes = [];
        
        foreach (var type in publicInteractionCommandTypes)
        {
            // Check type (class) for InteractionInstallTypeAttribute; if present, check if it allows user install
            if (type.GetCustomAttributes(typeof(InteractionInstallTypeAttribute), true).FirstOrDefault() is InteractionInstallTypeAttribute installTypeAttribute
                && installTypeAttribute.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall))
            {
                userInstallInteractionCommandTypes.Add(type);
            }
            else
            {
                // Check methods for InteractionInstallTypeAttribute; if present, check if it allows user install
                var methods = type.GetMethods();
                foreach (var method in methods)
                {
                    if (method.GetCustomAttributes(typeof(InteractionInstallTypeAttribute), true).FirstOrDefault() is InteractionInstallTypeAttribute methodInstallTypeAttribute
                        && methodInstallTypeAttribute.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall))
                    {
                        userInstallInteractionCommandTypes.Add(type);
                        break;
                    }
                }
            }
        }
        
        return userInstallInteractionCommandTypes;
    }
    
    /// <summary>
    ///     Gets a list of all interaction command types that allow guild-install and do not allow user-install. To be used for registering commands.
    /// </summary>
    /// <param name="publicInteractionCommandTypes">A list of all public interaction command types to filter through.</param>
    /// <returns>A list of all interaction command types that allow guild-install and do not allow user-install.</returns>
    private static List<Type> GetGuildInstallInteractionCommandTypes(List<Type> publicInteractionCommandTypes)
    {
        List<Type> guildInstallInteractionCommandTypes = [];
        
        foreach (var type in publicInteractionCommandTypes)
        {
            // Check type (class) for InteractionInstallTypeAttribute; if present, check if it allows guild install
            if (type.GetCustomAttributes(typeof(InteractionInstallTypeAttribute), true).FirstOrDefault() is InteractionInstallTypeAttribute installTypeAttribute
                && installTypeAttribute.InstallTypes.Contains(DiscordApplicationIntegrationType.GuildInstall)
                && !installTypeAttribute.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall))
            {
                guildInstallInteractionCommandTypes.Add(type);
            }
            else
            {
                // Check methods for InteractionInstallTypeAttribute; if present, check if it allows guild install
                var methods = type.GetMethods();
                foreach (var method in methods)
                {
                    if (method.GetCustomAttributes(typeof(InteractionInstallTypeAttribute), true).FirstOrDefault() is InteractionInstallTypeAttribute methodInstallTypeAttribute
                        && methodInstallTypeAttribute.InstallTypes.Contains(DiscordApplicationIntegrationType.GuildInstall)
                        && !methodInstallTypeAttribute.InstallTypes.Contains(DiscordApplicationIntegrationType.UserInstall))
                    {
                        guildInstallInteractionCommandTypes.Add(type);
                        break;
                    }
                }
            }
        }
        
        return guildInstallInteractionCommandTypes;
    }
    
    /// <summary>
    ///     Gets a list of all interaction command types that are only allowed in the home server. To be used for registering commands.
    /// </summary>
    /// <returns>A list of all interaction command types that are only allowed in the home server.</returns>
    private static List<Type> GetHomeServerInteractionCommandTypes()
    {
        return Assembly.GetExecutingAssembly().GetTypes().Where(t =>
            t.IsClass && t.Namespace is not null &&
            t.Namespace.Contains("MechanicalMilkshake.Commands.Owner.HomeServerCommands") &&
            !t.IsNested).ToList();
    }
}
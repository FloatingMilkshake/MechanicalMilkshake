namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class ReminderCommands
{
    // Used to pass context to modal handling
    // <user ID, message from context>
    public static Dictionary<ulong, DiscordMessage> ReminderInteractionCache = new();

    [Command("Remind Me About This")]
    [AllowedProcessors(typeof(MessageCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]

    public static async Task ContextReminder(MessageCommandContext ctx, DiscordMessage targetMessage)
    {
        await ctx.RespondWithModalAsync(new DiscordModalBuilder()
            .WithTitle("Remind Me About This")
            .WithCustomId("remind-me-about-this-modal")
            .AddTextInput(new DiscordTextInputComponent("remind-me-about-this-time-input"), "When do you want to be reminded?")
        );

        ReminderInteractionCache[ctx.User.Id] = targetMessage;
    }
}
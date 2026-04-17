namespace MechanicalMilkshake.Commands;

internal class CharacterCountCommands
{
    [Command("charactercount")]
    [Description("Counts the characters in a message.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task CharacterCountCommandAsync(SlashCommandContext ctx,
        [Parameter("message"), Description("The message to count the characters of.")]
        string chars)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent(chars.Length.ToString())
            .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
    }
}

namespace MechanicalMilkshake.Commands;

public class CharacterCount
{
    [Command("charactercount")]
    [Description("Counts the characters in a message.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task CharacterCountCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("message"), Description("The message to count the characters of.")]
        string chars)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(chars.Length.ToString()));
    }
}
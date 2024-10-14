namespace MechanicalMilkshake.Commands;

public class CharacterCount
{
    [Command("charactercount")]
    [Description("Counts the characters in a message.")]
    public static async Task CharacterCountCommand(SlashCommandContext ctx,
        [Parameter("message"), Description("The message to count the characters of.")]
        string chars)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(chars.Length.ToString()));
    }
}
namespace MechanicalMilkshake.Commands;

public class CharacterCount : ApplicationCommandModule
{
    [SlashCommand("charactercount", "Counts the characters in a message.")]
    public static async Task CharacterCountCommand(InteractionContext ctx,
        [Option("message", "The message to count the characters of.")]
        string chars)
    {
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(chars.Length.ToString()));
    }
}
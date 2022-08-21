namespace MechanicalMilkshake.Commands;

public class CharacterCount : ApplicationCommandModule
{
    [SlashCommand("charactercount", "Counts the characters in a message.")]
    public async Task CharacterCountCommand(InteractionContext ctx,
        [Option("message", "The message to count the characters of.")]
        string chars)
    {
        var count = 0;
        foreach (var chr in chars) count++;

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(count.ToString()));
    }
}
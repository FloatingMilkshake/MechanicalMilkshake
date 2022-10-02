namespace MechanicalMilkshake.Commands;

public class Feedback : ApplicationCommandModule
{
    [SlashCommand("feedback", "Have feedback about the bot? Submit it here!")]
    public async Task FeedbackCommand(InteractionContext ctx,
        [Option("message", "Your feedback message."), MaximumLength(4000)]
        string feedbackMsg)
    {
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder()
        {
            Title = "Thank you!", Color = Program.botColor,
            Description = $"Your feedback has been recorded. You can view it below.\n> {feedbackMsg}"
        }).AsEphemeral());
        var feedbackChannel = await ctx.Client.GetChannelAsync(1016805107993677926);
        DiscordEmbedBuilder embed = new()
        {
            Title = $"New feedback received!",
            Color = Program.botColor,
            Description = feedbackMsg
        };
        embed.AddField("Sent by", $"{ctx.User.Username}#{ctx.User.Discriminator} (`{ctx.User.Id}`)");
        embed.AddField("Sent from", $"\"{ctx.Guild.Name}\" (`{ctx.Guild.Id}`)");
        await feedbackChannel.SendMessageAsync(embed);
    }
}
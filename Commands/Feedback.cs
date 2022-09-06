namespace MechanicalMilkshake.Commands
{
    public class Feedback : ApplicationCommandModule
    {
        [SlashCommand("feedback", "Have feedback about the bot? Submit it here!")]
        public async Task FeedbackCommand(InteractionContext ctx,
            [Option("message", "Your feedback message.")] string feedbackMsg)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Thank you! Your feedback has been recorded. You can see it below.\n> {feedbackMsg}")
                .AsEphemeral());
            var feedbackChannel = await ctx.Client.GetChannelAsync(1016805107993677926);
            DiscordMessageBuilder message = new()
            {
                Content =
                    $"{ctx.User.Mention} (from \"{ctx.Guild.Name}\" (`{ctx.Guild.Id}`)):\n> {feedbackMsg}"
            };
            var msg = await feedbackChannel.SendMessageAsync(message.WithAllowedMentions(Mentions.None));
            await msg.ModifyEmbedSuppressionAsync(true);
        }
    }
}

namespace MechanicalMilkshake.Commands;

public class Feedback : ApplicationCommandModule
{
    [SlashCommand("feedback", "Have feedback about the bot? Submit it here!")]
    public static async Task FeedbackCommand(InteractionContext ctx,
        [Option("message", "Your feedback message.")] [MaximumLength(4000)]
        string feedbackMsg)
    {
        if (Program.DisabledCommands.Contains("feedback"))
        {
            await CommandHandlerHelpers.FailOnMissingInfo(ctx, false);
            return;
        }

        var feedbackChannelId = GetFeedbackChannelId();
        if (feedbackChannelId == default)
        {
            var aboutCmd = SlashCmdMentionHelpers.GetSlashCmdMention("about");
            
            await ctx.CreateResponseAsync(
                $"The feedback channel ID set in `config.json` is invalid! Please contact the bot owner;" +
                $" you can find who this is and how to contact them in {aboutCmd}.");
            return;
        }
        
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Thank you!", Color = Program.BotColor,
            Description = $"Your feedback has been recorded. You can view it below.\n> {feedbackMsg}"
        }).AsEphemeral());
        var feedbackChannel = await ctx.Client.GetChannelAsync(feedbackChannelId);
        DiscordEmbedBuilder embed = new()
        {
            Title = "New feedback received!",
            Color = Program.BotColor,
            Description = feedbackMsg
        };
        embed.AddField("Sent by", $"{ctx.User.Username}#{ctx.User.Discriminator} (`{ctx.User.Id}`)");
        embed.AddField("Sent from", $"\"{ctx.Guild.Name}\" (`{ctx.Guild.Id}`)");
        await feedbackChannel.SendMessageAsync(embed);
    }

    private static ulong GetFeedbackChannelId()
    {
        try
        {
            return Convert.ToUInt64(Program.ConfigJson.Ids.FeedbackChannel);
        }
        catch
        {
            return default;
        }
    }
}
using DSharpPlus.Commands.ArgumentModifiers;

namespace MechanicalMilkshake.Commands;

public class Feedback
{
    [Command("feedback")]
    [Description("Have feedback about the bot? Submit it here!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task FeedbackCommand(SlashCommandContext ctx,
        [Parameter("message"), Description("Your feedback message.")] [MinMaxLength(maxLength: 4000)]
        string feedbackMsg)
    {
        if (Program.DisabledCommands.Contains("feedback"))
        {
            await CommandHelpers.FailOnMissingInfo(ctx, false);
            return;
        }

        var feedbackChannelId = GetFeedbackChannelId();
        if (feedbackChannelId == default)
        {
            var aboutCmd = SlashCmdMentionHelpers.GetSlashCmdMention("about");
            
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(
                $"The feedback channel ID set in `config.json` is invalid! Please contact the bot owner;" +
                $" you can find who this is and how to contact them in {aboutCmd}.").AsEphemeral());
            return;
        }
        
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
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
        embed.AddField("Sent by", $"{UserInfoHelpers.GetFullUsername(ctx.User)} (`{ctx.User.Id}`)");
        if (ctx.Guild is not null)
            embed.AddField("Sent from", $"\"{ctx.Guild.Name}\" (`{ctx.Guild.Id}`)");
        await feedbackChannel.SendMessageAsync(embed);
    }

    private static ulong GetFeedbackChannelId()
    {
        try
        {
            return Convert.ToUInt64(Program.ConfigJson.FeedbackChannel);
        }
        catch
        {
            return default;
        }
    }
}
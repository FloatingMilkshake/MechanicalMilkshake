namespace MechanicalMilkshake.Commands;

internal class FeedbackCommands
{
    [Command("feedback")]
    [Description("Have feedback about the bot? Submit it here!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task FeedbackCommandAsync(SlashCommandContext ctx,
        [Parameter("message"), Description("Your feedback message.")] [MinMaxLength(maxLength: 4000)]
        string feedbackMsg)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(true));

        if (Setup.Configuration.Discord.Channels.Feedback == default)
        {
            await ctx.FollowupAsync(new DiscordInteractionResponseBuilder()
                .WithContent("Sorry, this command is unavailable! Please contact a bot owner for help.")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
            return;
        }

        DiscordEmbedBuilder embed = new()
        {
            Title = "New feedback received!",
            Color = Setup.Constants.BotColor,
            Description = feedbackMsg
        };
        embed.AddField("Sent by", $"{ctx.User.GetFullUsername()} (`{ctx.User.Id}`)");
        if (ctx.Guild is not null)
            embed.AddField("Sent from", $"\"{ctx.Guild.Name}\" (`{ctx.Guild.Id}`)");
        await Setup.Configuration.Discord.Channels.Feedback.SendMessageAsync(embed);

        await ctx.FollowupAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
        {
            Title = "Thank you!",
            Color = Setup.Constants.BotColor,
            Description = $"Your feedback has been sent."
        }).AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
    }
}

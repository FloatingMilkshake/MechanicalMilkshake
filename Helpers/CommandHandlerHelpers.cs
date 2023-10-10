namespace MechanicalMilkshake.Helpers;

public class CommandHandlerHelpers
{
    /// <summary>
    ///     Responds to an interaction with a failure message if the command is disabled.
    ///     Commands are disabled if required configuration information is missing.
    /// </summary>
    /// <param name="ctx">Interaction context used to respond to the interaction.</param>
    /// <param name="isFollowUp">Whether to follow-up to the interaction (as opposed to creating a new interaction response).</param>
    public static async Task FailOnMissingInfo(InteractionContext ctx, bool isFollowUp)
    {
        const string failureMsg =
            "This command is disabled! Please make sure you have provided values for all of the necessary keys in the config file.";

        if (isFollowUp)
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(failureMsg));
        else
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(failureMsg));
    }
}
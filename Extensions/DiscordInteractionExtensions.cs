namespace MechanicalMilkshake.Extensions;

internal static class DiscordInteractionExtensions
{
    extension (DiscordInteraction interaction)
    {
        internal async Task SmartRespondAsync(string message)
        {
            var interactionResponseState = interaction.ResponseState;

            if (interactionResponseState != DiscordInteractionResponseState.Unacknowledged &&
                interactionResponseState != DiscordInteractionResponseState.Deferred &&
                interactionResponseState != DiscordInteractionResponseState.Replied)
            {
                return;
            }

            if (interactionResponseState == DiscordInteractionResponseState.Unacknowledged)
            {
                await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(message));
            }
            else // Deferred
            {
                await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(message));
            }
        }
    }
}

namespace MechanicalMilkshake.Extensions;

internal static class DiscordInteractionExtensions
{
    extension(DiscordInteraction interaction)
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

        internal bool ShouldUseEphemeralResponse(bool preferEphemeral)
            => preferEphemeral || IsUserInstallContext(interaction);

        internal bool IsUserInstallContext()
        {
            if (interaction.GuildId is not null && !Setup.State.Discord.Client.Guilds.ContainsKey(interaction.GuildId.Value))
                return true;

            if (interaction.Context == DiscordInteractionContextType.BotDM)
                return false;

            if (interaction.Context != DiscordInteractionContextType.Guild)
                return true;

            return false;
        }
    }
}

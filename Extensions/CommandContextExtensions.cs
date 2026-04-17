namespace MechanicalMilkshake.Extensions;

internal static class CommandContextExtensions
{
    internal static bool ShouldUseEphemeralResponse(this SlashCommandContext ctx, bool preferEphemeral = false)
        => ShouldUseEphemeralResponse(ctx.Interaction, preferEphemeral);

    internal static bool ShouldUseEphemeralResponse(this MessageCommandContext ctx, bool preferEphemeral = false)
        => ShouldUseEphemeralResponse(ctx.Interaction, preferEphemeral);

    internal static bool ShouldUseEphemeralResponse(this UserCommandContext ctx, bool preferEphemeral = false)
        => ShouldUseEphemeralResponse(ctx.Interaction, preferEphemeral);

    private static bool ShouldUseEphemeralResponse(DiscordInteraction interaction, bool preferEphemeral)
    {
        if (preferEphemeral)
            return true;

        if (interaction.GuildId is not null && !Setup.State.Discord.Client.Guilds.ContainsKey(interaction.GuildId.Value))
            return true;

        if (interaction.Context == DiscordInteractionContextType.BotDM)
            return false;

        if (interaction.Context != DiscordInteractionContextType.Guild)
            return true;

        return false;
    }
}

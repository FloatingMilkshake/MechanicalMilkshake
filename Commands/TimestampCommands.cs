namespace MechanicalMilkshake.Commands;

[Command("timestamp")]
[Description("Returns the Unix timestamp of a given date.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
[InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
internal class TimestampCommands
{
    [Command("id")]
    [Description("Returns the Unix timestamp of a given Discord ID/snowflake.")]
    public static async Task TimestampIdCommandAsync(SlashCommandContext ctx,
        [Parameter("snowflake"), Description("The ID/snowflake to fetch the Unix timestamp for.")]
        string id,
        [SlashChoiceProvider(typeof(Setup.Types.ChoiceProviders.TimestampFormatChoiceProvider))]
        [Parameter("format"), Description("The format to convert the timestamp to.")]
        string format = "",
        [Parameter("include_code"), Description("Whether to include the code for the timestamp.")]
        bool includeCode = false)
    {
        ulong snowflake;
        try
        {
            snowflake = Convert.ToUInt64(id);
        }
        catch (FormatException)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent("Hmm, that doesn't look like a valid ID/snowflake. I wasn't able to convert it to a timestamp.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            return;
        }

        var timestamp = snowflake.ToUnixTimeSeconds();
        if (string.IsNullOrWhiteSpace(format))
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"{timestamp}")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
        else
        {
            if (includeCode)
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"<t:{timestamp}:{format}> (`<t:{timestamp}:{format}>`)")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            else
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"<t:{timestamp}:{format}>")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
    }

    [Command("date")]
    [Description("Returns the Unix timestamp of a given date.")]
    public static async Task TimestampDateCommandAsync(SlashCommandContext ctx,
        [Parameter("date"), Description("The date to fetch the Unix timestamp for.")]
        string date,
        [SlashChoiceProvider(typeof(Setup.Types.ChoiceProviders.TimestampFormatChoiceProvider))]
        [Parameter("format"), Description("The format to convert the timestamp to. Options are F/D/T/R/f/d/t.")]
        string format = "",
        [Parameter("include_code"), Description("Whether to include the code for the timestamp.")]
        bool includeCode = false)
    {
        long unixTime;
        try
        {
            unixTime = Convert.ToDateTime(date).ToUnixTimeSeconds();
        }
        catch (FormatException)
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent("Hmm, that doesn't look like a valid date. I wasn't able to convert it to a timestamp.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            return;
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"{unixTime}")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
        else
        {
            if (includeCode)
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"<t:{unixTime}:{format}> (`<t:{unixTime}:{format}>`)")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            else
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                    .WithContent($"<t:{unixTime}:{format}>")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        }
    }
}

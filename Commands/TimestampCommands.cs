namespace MechanicalMilkshake.Commands;

[Command("timestamp")]
[Description("Returns the Unix timestamp of a given date.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
[InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
public class TimestampCmds
{
    [Command("id")]
    [Description("Returns the Unix timestamp of a given Discord ID/snowflake.")]
    public static async Task TimestampSnowflakeCmd(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("snowflake"), Description("The ID/snowflake to fetch the Unix timestamp for.")]
        string id,
        [SlashChoiceProvider(typeof(TimestampFormatChoiceProvider))]
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
        catch
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(
                "Hmm, that doesn't look like a valid ID/snowflake. I wasn't able to convert it to a timestamp."));
            return;
        }

        var timestamp = IdHelpers.GetCreationTimestamp(snowflake, true);
        if (string.IsNullOrWhiteSpace(format))
        {
            await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent($"{timestamp}"));
        }
        else
        {
            if (includeCode)
                await ctx.RespondAsync(
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"<t:{timestamp}:{format}> (`<t:{timestamp}:{format}>`)"));
            else
                await ctx.RespondAsync(
                    new DiscordInteractionResponseBuilder().WithContent($"<t:{timestamp}:{format}>"));
        }
    }

    [Command("date")]
    [Description("Returns the Unix timestamp of a given date.")]
    public static async Task TimestampDateCmd(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("date"), Description("The date to fetch the Unix timestamp for.")]
        string date,
        [SlashChoiceProvider(typeof(TimestampFormatChoiceProvider))]
        [Parameter("format"), Description("The format to convert the timestamp to. Options are F/D/T/R/f/d/t.")]
        string format = "",
        [Parameter("include_code"), Description("Whether to include the code for the timestamp.")]
        bool includeCode = false)
    {
        long unixTime;
        try
        {
            var dateToConvert = Convert.ToDateTime(date);
            unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();
        }
        catch
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent(
                "Hmm, that doesn't look like a valid date. I wasn't able to convert it to a timestamp."));
            return;
        }

        if (string.IsNullOrWhiteSpace(format))
        {
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent($"{unixTime}"));
        }
        else
        {
            if (includeCode)
                await ctx.RespondAsync(
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"<t:{unixTime}:{format}> (`<t:{unixTime}:{format}>`)"));
            else
                await ctx.RespondAsync(
                    new DiscordInteractionResponseBuilder().WithContent($"<t:{unixTime}:{format}>"));
        }
    }
    
    private class TimestampFormatChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("Short Time", "t"),
            new("Long Time", "T"),
            new("Short Date", "d"),
            new("Long Date", "D"),
            new("Short Date/Time", "f"),
            new("Long Date/Time", "F"),
            new("Relative Time", "R"),
            new("Raw Timestamp", "")
        ];
        
        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }
}
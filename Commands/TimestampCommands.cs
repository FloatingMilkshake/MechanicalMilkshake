namespace MechanicalMilkshake.Commands;

public class TimestampCommands : ApplicationCommandModule
{
    [SlashCommandGroup("timestamp", "Returns the Unix timestamp of a given date.")]
    private class TimestampCmds : ApplicationCommandModule
    {
        [SlashCommand("id", "Returns the Unix timestamp of a given Discord ID/snowflake.")]
        public static async Task TimestampSnowflakeCmd(InteractionContext ctx,
            [Option("snowflake", "The ID/snowflake to fetch the Unix timestamp for.")]
            string id,
            [Choice("Short Time", "t")]
            [Choice("Long Time", "T")]
            [Choice("Short Date", "d")]
            [Choice("Long Date", "D")]
            [Choice("Short Date/Time", "f")]
            [Choice("Long Date/Time", "F")]
            [Choice("Relative Time", "R")]
            [Choice("Raw Timestamp", "")]
            [Option("format", "The format to convert the timestamp to.")]
            string format = "",
            [Option("include_code", "Whether to include the code for the timestamp.")]
            bool includeCode = false)
        {
            ulong snowflake;
            try
            {
                snowflake = Convert.ToUInt64(id);
            }
            catch
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    "Hmm, that doesn't look like a valid ID/snowflake. I wasn't able to convert it to a timestamp."));
                return;
            }

            var timestamp = IdHelpers.GetCreationTimestamp(snowflake, true);
            if (string.IsNullOrWhiteSpace(format))
            {
                await ctx.CreateResponseAsync(
                    new DiscordInteractionResponseBuilder().WithContent($"{timestamp}"));
            }
            else
            {
                if (includeCode)
                    await ctx.CreateResponseAsync(
                        new DiscordInteractionResponseBuilder().WithContent(
                            $"<t:{timestamp}:{format}> (`<t:{timestamp}:{format}>`)"));
                else
                    await ctx.CreateResponseAsync(
                        new DiscordInteractionResponseBuilder().WithContent($"<t:{timestamp}:{format}>"));
            }
        }

        [SlashCommand("date", "Returns the Unix timestamp of a given date.")]
        public static async Task TimestampDateCmd(InteractionContext ctx,
            [Option("date", "The date to fetch the Unix timestamp for.")]
            string date,
            [Choice("Short Time", "t")]
            [Choice("Long Time", "T")]
            [Choice("Short Date", "d")]
            [Choice("Long Date", "D")]
            [Choice("Short Date/Time", "f")]
            [Choice("Long Date/Time", "F")]
            [Choice("Relative Time", "R")]
            [Choice("Raw Timestamp", "")]
            [Option("format", "The format to convert the timestamp to. Options are F/D/T/R/f/d/t.")]
            string format = "",
            [Option("include_code", "Whether to include the code for the timestamp.")]
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
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    "Hmm, that doesn't look like a valid date. I wasn't able to convert it to a timestamp."));
                return;
            }

            if (string.IsNullOrWhiteSpace(format))
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{unixTime}"));
            }
            else
            {
                if (includeCode)
                    await ctx.CreateResponseAsync(
                        new DiscordInteractionResponseBuilder().WithContent(
                            $"<t:{unixTime}:{format}> (`<t:{unixTime}:{format}>`)"));
                else
                    await ctx.CreateResponseAsync(
                        new DiscordInteractionResponseBuilder().WithContent($"<t:{unixTime}:{format}>"));
            }
        }
    }
}
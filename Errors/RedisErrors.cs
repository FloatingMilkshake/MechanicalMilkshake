namespace MechanicalMilkshake.Errors;

internal class RedisErrors
{
    internal static async Task HandleRedisTimeoutExceptionAsync(RedisTimeoutException ex)
    {
        // use double.NaN as comparison value for unreachable
        double dbPing = double.NaN;
        try
        {
            // attempt to ping db
            dbPing = (await Setup.Storage.Redis.PingAsync()).TotalMilliseconds;
        }
        catch (RedisTimeoutException)
        {
            // db ping failed

            // if exceptions are already suppressed, don't report
            if (Setup.State.Process.RedisExceptionsSuppressed)
                return;

            // report and suppress further timeout exceptions

            var ownerMention = string.Join(" ", Setup.State.Discord.Client.CurrentApplication.Owners.Select(o => o.Mention));

            var pingMsg = double.IsNaN(dbPing)
                ? "I couldn't ping redis."
                : $"Redis is reachable, and took {dbPing}ms to respond.";
            await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(
                $"{ownerMention} Redis is timing out! {pingMsg}" +
                $" Redis exceptions will be suppressed until the next check.",
                embed: new DiscordEmbedBuilder
                {
                    Color = DiscordColor.Red,
                    Title = "A redis error occurred",
                    Description = $"`{ex.GetType()}: {ex.Message}`"
                });

            Setup.State.Discord.Client.Logger.LogError(ex, "A redis error occurred!");

            Setup.State.Process.RedisExceptionsSuppressed = true;
        }
    }
}

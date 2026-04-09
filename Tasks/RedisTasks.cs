namespace MechanicalMilkshake.Tasks;

internal class RedisTasks
{
    internal static async Task ExecuteAsync()
    {
        while (true)
        {
            await CheckRedisConnectionAsync();
            await Task.Delay(TimeSpan.FromSeconds(5));
        }
    }

    internal static async Task<double> CheckRedisConnectionAsync()
    {
        var dbPing = double.NaN;
        try
        {
            dbPing = (await Setup.Storage.Redis.PingAsync()).TotalMilliseconds;
        }
        catch (RedisTimeoutException ex)
        {
            await Errors.RedisErrors.HandleRedisTimeoutExceptionAsync(ex);
        }

        // un-suppress exceptions if redis is reachable & they are currently suppressed
        if (Setup.State.Process.RedisExceptionsSuppressed && !double.IsNaN(dbPing))
        {
            Setup.State.Process.RedisExceptionsSuppressed = false;
            await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(
                "Redis was previously unreachable but is now reachable." +
                $" Exceptions will no longer be suppressed. Current ping: `{dbPing}ms`.");
        }

        return dbPing;
    }
}

namespace MechanicalMilkshake.Checks;

public class DatabaseChecks
{
    public static async Task<double> CheckDatabaseConnectionAsync()
    {
        var dbPing = double.NaN;
        try
        {
            dbPing = (await Program.Db.PingAsync()).TotalMilliseconds;
        }
        catch (Exception ex)
        {
            await ErrorEvents.DatabaseConnectionErrored(ex);
        }
        
        // un-suppress exceptions if redis is reachable & they are currently suppressed
        if (Program.RedisExceptionsSuppressed && !double.IsNaN(dbPing))
        {
            Program.RedisExceptionsSuppressed = false;
            await Program.HomeChannel.SendMessageAsync(
                "Redis was previously unreachable but is now reachable." +
                $" Exceptions will no longer be suppressed. Current ping: `{dbPing}ms`.");
        }

        return dbPing;
    }
}
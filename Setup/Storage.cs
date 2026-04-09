namespace MechanicalMilkshake.Setup;

internal class Storage
{
#if DEBUG
    private static readonly ConnectionMultiplexer RedisConnection = ConnectionMultiplexer.Connect("localhost:6379");
#else
    private static readonly ConnectionMultiplexer RedisConnection = ConnectionMultiplexer.Connect("redis");
#endif
    internal static readonly IDatabase Redis = RedisConnection.GetDatabase();
}

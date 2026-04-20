namespace MechanicalMilkshake.Setup.State;

internal static class Process
{
    internal static Setup.Types.ConfigJson Configuration;
    internal static bool RedisExceptionsSuppressed;
    internal static readonly DateTime ProcessStartTime = DateTime.Now;
    internal static string LastUptimeKumaHeartbeatStatus = "waiting";
}

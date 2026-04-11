namespace MechanicalMilkshake.Helpers;

internal class DateHelpers
{
    internal static long GetUnixTimestamp(ulong userId)
    {
        return (long)(((userId >> 22) + 1420070400000) / 1000);
    }

    internal static long GetUnixTimestamp(string dateTime)
    {
        return GetUnixTimestamp(Convert.ToDateTime(dateTime));
    }

    internal static long GetUnixTimestamp(DateTime dateTime)
    {
        return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
    }

    internal static long GetUnixTimestamp(DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToUnixTimeSeconds();
    }
}

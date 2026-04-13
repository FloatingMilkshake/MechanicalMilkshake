namespace MechanicalMilkshake.Extensions;

internal static class DateTimeExtensions
{
    extension(DateTime dateTime)
    {
        internal long ToUnixTimeSeconds()
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }
    }
}

namespace MechanicalMilkshake.Extensions;

internal static class DateTimeExtensions
{
    extension(DateTime dateTime)
    {
        internal long ToUnixTimeSeconds()
        {
            return ((DateTimeOffset)dateTime).ToUnixTimeSeconds();
        }

        internal string Humanize()
        {
            TimeSpan diff = DateTime.UtcNow - dateTime;
            string relative = diff.Duration().Humanize();
            return diff >= TimeSpan.Zero ? $"{relative} ago" : $"in {relative}";
        }
    }
}

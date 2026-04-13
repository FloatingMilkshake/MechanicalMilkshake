namespace MechanicalMilkshake.Extensions;

internal static class UlongExtensions
{
    extension(ulong u)
    {
        internal long ToUnixTimeSeconds()
        {
            return (long)(((u >> 22) + 1420070400000) / 1000);
        }
    }
}

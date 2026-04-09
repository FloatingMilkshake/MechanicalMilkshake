namespace MechanicalMilkshake.Helpers;

internal class IdHelpers
{
    internal static ulong GetCreationTimestamp(ulong userId, bool useMilliseconds)
    {
        return useMilliseconds ? ((userId >> 22) + 1420070400000) / 1000 : (userId >> 22) + 1420070400000;
    }
}

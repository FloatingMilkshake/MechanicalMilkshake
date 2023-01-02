namespace MechanicalMilkshake.Helpers;

public class IdHelpers
{
    /// <summary>
    ///     Generate a creation timestamp from a user's Discord ID.
    ///     Can be used to create dynamic timestamps. See Commands/TimestampCommands:9 and:
    ///     https://discord.com/developers/docs/reference#snowflakes-snowflake-id-format-structure-left-to-right
    ///     https://discord.com/developers/docs/reference#message-formatting-formats
    /// </summary>
    /// <param name="userId">The User ID to get the creation timestamp for.</param>
    /// <param name="useMilliseconds">Whether to return a value in milliseconds versus seconds.</param>
    /// <returns>The creation timestamp for the ID provided, in either seconds or milliseconds.</returns>
    public static ulong GetCreationTimestamp(ulong userId, bool useMilliseconds)
    {
        return useMilliseconds ? ((userId >> 22) + 1420070400000) / 1000 : (userId >> 22) + 1420070400000;
    }
}
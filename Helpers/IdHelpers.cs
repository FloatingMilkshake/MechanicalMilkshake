namespace MechanicalMilkshake.Helpers;

public class IdHelpers
{
    /*
     * Generate a creation timestamp from a user's Discord ID.
     * Can be used to create dynamic timestamps. See Commands/TimestampCommands:9 and:
     * https://discord.com/developers/docs/reference#snowflakes-snowflake-id-format-structure-left-to-right
     * https://discord.com/developers/docs/reference#message-formatting-formats
     */
    public static ulong GetCreationTimestamp(ulong userId)
    {
        return ((userId >> 22) + 1420070400000) / 1000;
    }
}
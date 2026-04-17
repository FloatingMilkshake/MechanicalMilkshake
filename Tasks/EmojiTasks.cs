namespace MechanicalMilkshake.Tasks;

internal static class EmojiTasks
{
    internal static async Task ExecuteAsync()
    {
        await PopulateApplicationEmojiListAsync();
    }

    private static async Task PopulateApplicationEmojiListAsync()
    {
        Setup.State.Discord.ApplicationEmoji.AddRange(await Setup.State.Discord.Client.GetApplicationEmojisAsync());
    }
}

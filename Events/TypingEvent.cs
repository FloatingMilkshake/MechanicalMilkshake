namespace MechanicalMilkshake.Events;

public class TypingEvent
{
    public static List<KeyValuePair<DiscordUser, DiscordChannel>> TypingUsers = new();
    
    public static async Task TypingStarted(DiscordClient _, TypingStartEventArgs e)
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () =>
        {
            KeyValuePair<DiscordUser, DiscordChannel> kv = new(e.User, e.Channel);
            TypingUsers.Add(kv);
            await Task.Delay(10000);
            TypingUsers.Remove(kv);
        });
    }
}
namespace MechanicalMilkshake.Events;

public class TypingEvent
{
    public static readonly List<KeyValuePair<DiscordUser, DiscordChannel>> TypingUsers = new();

    public static Task TypingStarted(DiscordClient _, TypingStartEventArgs e)
    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        Task.Run(async () =>
        {
            if (e.User is null || e.Channel is null) return; // prevents NREs later
            
            KeyValuePair<DiscordUser, DiscordChannel> kv = new(e.User, e.Channel);
            TypingUsers.Add(kv);
            await Task.Delay(10000);
            TypingUsers.Remove(kv);
        });
        return Task.CompletedTask;
    }
}
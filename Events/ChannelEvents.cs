namespace MechanicalMilkshake.Events;

public class ChannelEvents
{
    public static Task ChannelDeleted(DiscordClient client, ChannelDeleteEventArgs e)
    {
        Task.Run(async () =>
        {
            // If channel has a message in cache, remove
            Program.LastMessageCache.Remove(e.Channel.Id);
        });
        return Task.CompletedTask;
    }
}
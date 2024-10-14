namespace MechanicalMilkshake.Events;

public class ChannelEvents
{
    public static Task ChannelDeleted(DiscordClient client, ChannelDeletedEventArgs e)
    {
        Task.Run(async () =>
        {
            // If channel has a message in cache, remove
            Program.MessageCache.RemoveChannel(e.Channel.Id);
        });
        return Task.CompletedTask;
    }
}
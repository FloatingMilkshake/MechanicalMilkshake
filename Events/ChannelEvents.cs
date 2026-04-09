namespace MechanicalMilkshake.Events;

internal class ChannelEvents
{
    internal static async Task HandleChannelDeletedEventAsync(DiscordClient _, ChannelDeletedEventArgs e)
    {
        // If channel has a message in cache, remove
        Setup.State.Caches.MessageCache.RemoveChannel(e.Channel.Id);
    }
}

namespace MechanicalMilkshake.Entities.MessageCaching;

public class CachedMessage
{
    public CachedMessage(ulong channelId, ulong messageId, ulong authorId)
    {
        ChannelId = channelId;
        MessageId = messageId;
        AuthorId = authorId;
    }

    public ulong ChannelId { get; }
    public ulong MessageId { get; }
    public ulong AuthorId { get; }
}
namespace MechanicalMilkshake.Setup.Types;

internal sealed class MessageCache
{
    internal MessageCache()
    {
        Messages = [];
    }

    internal bool TryGetMessage(ulong messageId, out CachedMessage message)
    {
        message = GetMessage(messageId);
        return message != null;
    }

    internal bool TryGetMessageByChannel(ulong channelId, out CachedMessage message)
    {
        message = GetMessageByChannel(channelId);
        return message != null;
    }

    internal bool TryGetMessageByAuthor(ulong authorId, out CachedMessage message)
    {
        message = GetMessageByAuthor(authorId);
        return message != null;
    }

    internal CachedMessage GetMessage(ulong messageId)
    {
        return Messages.Find(x => x.MessageId == messageId);
    }

    internal CachedMessage GetMessageByChannel(ulong channelId)
    {
        return Messages.Find(x => x.ChannelId == channelId);
    }

    internal CachedMessage GetMessageByAuthor(ulong authorId)
    {
        return Messages.Find(x => x.AuthorId == authorId);
    }

    internal List<CachedMessage> GetAllMessages()
    {
        return Messages;
    }

    internal int Count()
    {
        return Messages.Count;
    }

    internal int Count(Func<CachedMessage, bool> predicate)
    {
        return Messages.Count(predicate);
    }

    internal void AddMessage(CachedMessage message)
    {
        if (TryGetMessageByChannel(message.ChannelId, out var _))
            RemoveChannel(message.ChannelId);

        Messages.Add(message);
    }

    internal void RemoveMessage(ulong messageId)
    {
        Messages.RemoveAll(x => x.MessageId == messageId);
    }

    internal void RemoveChannel(ulong channelId)
    {
        Messages.RemoveAll(x => x.ChannelId == channelId);
    }

    internal void RemoveAuthor(ulong authorId)
    {
        Messages.RemoveAll(x => x.AuthorId == authorId);
    }

    private List<CachedMessage> Messages { get; }

    internal sealed class CachedMessage
    {
        internal ulong ChannelId { get; private set; }
        internal ulong MessageId { get; private set; }
        internal ulong AuthorId { get; private set; }

        internal CachedMessage(ulong channelId, ulong messageId, ulong authorId)
        {
            ChannelId = channelId;
            MessageId = messageId;
            AuthorId = authorId;
        }
    }
}

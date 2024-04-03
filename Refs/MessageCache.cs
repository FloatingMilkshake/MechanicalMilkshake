namespace MechanicalMilkshake.Refs;

public class MessageCache
{
    // Constructors
    
    // Init cache with list
    public MessageCache(List<CachedMessage> messages)
    {
        Messages = messages;
    }

    // Init new/empty cache
    public MessageCache()
    {
        Messages = [];
    }
    
    // Get data from cache
    
    // Equivalent to List<T>().TryGetValue(TKey, out TValue)
    public bool TryGetMessage(ulong messageId, out CachedMessage message)
    {
        message = GetMessage(messageId);
        return message != null;
    }
    
    // Same as above but checking by channel ID
    public bool TryGetMessageByChannel(ulong channelId, out CachedMessage message)
    {
        message = GetMessageByChannel(channelId);
        return message != null;
    }
    
    // Same as above but checking by author ID
    public bool TryGetMessageByAuthor(ulong authorId, out CachedMessage message)
    {
        message = GetMessageByAuthor(authorId);
        return message != null;
    }
    
    // Get message by ID
    public CachedMessage GetMessage(ulong messageId)
    {
        return Messages.Find(x => x.MessageId == messageId);
    }
    
    // Get message by chan ID
    public CachedMessage GetMessageByChannel(ulong channelId)
    {
        return Messages.Find(x => x.ChannelId == channelId);
    }
    
    // Get message by author ID
    public CachedMessage GetMessageByAuthor(ulong authorId)
    {
        return Messages.Find(x => x.AuthorId == authorId);
    }
    
    // Get entire cache
    public List<CachedMessage> GetAllMessages()
    {
        return Messages;
    }
    
    // Get count of messages in cache
    public int Count()
    {
        return Messages.Count;
    }
    
    // Modify cache
    
    // Add message to cache
    public void AddMessage(CachedMessage message)
    {
        if (TryGetMessageByChannel(message.ChannelId, out var _))
            RemoveChannel(message.ChannelId);
        
        Messages.Add(message);
    }
    
    // Remove message from cache by message ID
    public void RemoveMessage(ulong messageId)
    {
        Messages.RemoveAll(x => x.MessageId == messageId);
    }

    // Remove message from cache by chan ID
    public void RemoveChannel(ulong channelId)
    {
        Messages.RemoveAll(x => x.ChannelId == channelId);
    }

    // Remove message from cache by author ID
    public void RemoveAuthor(ulong authorId)
    {
        Messages.RemoveAll(x => x.AuthorId == authorId);
    }

    private List<CachedMessage> Messages { get; }
}
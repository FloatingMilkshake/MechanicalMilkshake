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

    internal CachedMessage GetNewestMessage(int skip = 0)
    {
        return GetAllMessages().OrderByDescending(m => m.MessageId).Skip(skip).First();
    }

    internal CachedMessage GetOldestMessage(int skip = 0)
    {
        return GetAllMessages().OrderBy(m => m.MessageId).Skip(skip).First();
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

    internal int GetUniqueChannelCount()
    {
        List<ulong> uniqueChannelIds = [];

        foreach (var cachedMessage in GetAllMessages())
        {
            if (!uniqueChannelIds.Contains(cachedMessage.ChannelId))
                uniqueChannelIds.Add(cachedMessage.ChannelId);
        }

        return uniqueChannelIds.Count;
    }

    internal int GetUniqueAuthorCount()
    {
        List<ulong> uniqueAuthorIds = [];

        foreach (var cachedMessage in GetAllMessages())
        {
            if (!uniqueAuthorIds.Contains(cachedMessage.AuthorId))
                uniqueAuthorIds.Add(cachedMessage.AuthorId);
        }

        return uniqueAuthorIds.Count;
    }

    internal void AddMessage(DiscordMessage message)
    {
        if (TryGetMessageByChannel(message.ChannelId, out var _))
            RemoveChannel(message.ChannelId);

        Messages.Add(new CachedMessage(message));
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

        internal CachedMessage(DiscordMessage message)
        {
            ChannelId = message.ChannelId;
            MessageId = message.Id;
            AuthorId = message.Author.Id;
        }

        internal string GetTimestamp()
        {
            return $"<t:{MessageId.ToUnixTimeSeconds()}:f>";
        }

        internal async Task<string> GetMessageLinkAsync()
        {
            var guildId = (await Setup.State.Discord.Client.GetChannelAsync(ChannelId)).GuildId;
            return $"https://discord.com/channels/{guildId}/{ChannelId}/{MessageId}";
        }

        internal async Task<string> GetInformationAsync()
        {
            DiscordChannel channel = default;
            DiscordUser author = default;
            try
            {
                channel = await Setup.State.Discord.Client.GetChannelAsync(ChannelId);
                author = await Setup.State.Discord.Client.GetUserAsync(AuthorId);
            }
            catch (Exception ex) when (ex is NotFoundException or UnauthorizedException)
            {
                // Don't care
            }
            string channelInformation = "**Channel:** ";
            if (channel == default)
                channelInformation += ChannelId;
            else
                channelInformation += $"{channel.Name} {channel.Id}";
            string authorInformation = "**Author:** ";
            if (author == default)
                authorInformation += AuthorId;
            else
                authorInformation += $"{author.Username} {author.Id}";


            return $"**Timestamp:** {GetTimestamp()}"
                + $"\n**Message ID:** {MessageId}"
                + $"\n{channelInformation}"
                + $"\n{authorInformation}"
                + $"\n{await GetMessageLinkAsync()}";
        }
    }
}

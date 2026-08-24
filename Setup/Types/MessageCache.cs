namespace MechanicalMilkshake.Setup.Types;

public sealed class MessageCache
{
    public MessageCache()
    {
        Messages = [];
    }

    public bool TryGetMessage(ulong messageId, out CachedMessage message)
    {
        message = GetMessage(messageId);
        return message != null;
    }

    public bool TryGetMessageByChannel(ulong channelId, out CachedMessage message)
    {
        message = GetMessageByChannel(channelId);
        return message != null;
    }

    public bool TryGetMessageByAuthor(ulong authorId, out CachedMessage message)
    {
        message = GetMessageByAuthor(authorId);
        return message != null;
    }

    public CachedMessage GetMessage(ulong messageId)
    {
        return Messages.Find(x => x.MessageId == messageId);
    }

    public CachedMessage GetMessageByChannel(ulong channelId)
    {
        return Messages.Find(x => x.ChannelId == channelId);
    }

    public CachedMessage GetMessageByAuthor(ulong authorId)
    {
        return Messages.Find(x => x.AuthorId == authorId);
    }

    public CachedMessage GetNewestMessage(int skip = 0)
    {
        return GetAllMessages().OrderByDescending(m => m.MessageId).Skip(skip).First();
    }

    public CachedMessage GetOldestMessage(int skip = 0)
    {
        return GetAllMessages().OrderBy(m => m.MessageId).Skip(skip).First();
    }

    public List<CachedMessage> GetAllMessages()
    {
        return Messages;
    }

    public int Count()
    {
        return Messages.Count;
    }

    public int Count(Func<CachedMessage, bool> predicate)
    {
        return Messages.Count(predicate);
    }

    public int GetUniqueChannelCount()
    {
        List<ulong> uniqueChannelIds = [];

        foreach (var cachedMessage in GetAllMessages())
        {
            if (!uniqueChannelIds.Contains(cachedMessage.ChannelId))
                uniqueChannelIds.Add(cachedMessage.ChannelId);
        }

        return uniqueChannelIds.Count;
    }

    public async Task<int> GetUniqueGuildCountAsync()
    {
        List<ulong> uniqueGuildIds = [];

        foreach (var cachedMessage in GetAllMessages())
        {
            var guildId = Setup.State.Discord.Client.Guilds.Values.FirstOrDefault(g => g.Channels.Any(c => c.Value.Id == cachedMessage.ChannelId))?.Id ?? default;
            if (guildId == default)
                guildId = (await Setup.State.Discord.Client.GetChannelAsync(cachedMessage.ChannelId)).Guild.Id;
            if (!uniqueGuildIds.Contains(guildId))
                uniqueGuildIds.Add(guildId);
        }

        return uniqueGuildIds.Count;
    }

    public int GetUniqueAuthorCount()
    {
        List<ulong> uniqueAuthorIds = [];

        foreach (var cachedMessage in GetAllMessages())
        {
            if (!uniqueAuthorIds.Contains(cachedMessage.AuthorId))
                uniqueAuthorIds.Add(cachedMessage.AuthorId);
        }

        return uniqueAuthorIds.Count;
    }

    public void AddMessage(DiscordMessage message)
    {
        if (TryGetMessageByChannel(message.ChannelId, out var _))
            RemoveChannel(message.ChannelId);

        Messages.Add(new CachedMessage(message));
    }

    public void RemoveMessage(ulong messageId)
    {
        Messages.RemoveAll(x => x.MessageId == messageId);
    }

    public void RemoveChannel(ulong channelId)
    {
        Messages.RemoveAll(x => x.ChannelId == channelId);
    }

    public void RemoveAuthor(ulong authorId)
    {
        Messages.RemoveAll(x => x.AuthorId == authorId);
    }

    public List<CachedMessage> Messages { get; }

    public sealed class CachedMessage
    {
        public ulong ChannelId { get; set; }
        public ulong MessageId { get; set; }
        public ulong AuthorId { get; set; }

        public CachedMessage(DiscordMessage message)
        {
            ChannelId = message.ChannelId;
            MessageId = message.Id;
            AuthorId = message.Author.Id;
        }

        public string GetTimestamp()
        {
            return $"<t:{MessageId.ToUnixTimeSeconds()}:f>";
        }

        public async Task<string> GetMessageLinkAsync()
        {
            var guildId = (await Setup.State.Discord.Client.GetChannelAsync(ChannelId)).GuildId;
            return $"https://discord.com/channels/{guildId}/{ChannelId}/{MessageId}";
        }

        public async Task<string> GetInformationAsync()
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

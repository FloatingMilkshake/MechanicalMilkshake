namespace MechanicalMilkshake.Extensions;

internal static class DiscordMessageExtensions
{
    extension(DiscordMessage message)
    {
        internal async Task CheckForTrackedKeywordsAsync(bool isEdit = false)
        {
            if (message.Author is null || message.Content is null)
                return;

            if (message.Author.Id == Setup.State.Discord.Client.CurrentUser.Id)
                return;

            if (message.Channel.IsPrivate)
                return;

            if (isEdit && (message.EditedTimestamp is null || message.CreationTimestamp == message.EditedTimestamp || message.EditedTimestamp.Value.UtcDateTime < (DateTime.UtcNow - TimeSpan.FromMinutes(1))))
                return;

            var allKeywords = (await Setup.Storage.Redis.HashGetAllAsync("keywords")).Select(k => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(k.Value));

            // Stop! Before we make ANY API calls, does this message even contain any keywords?
            if (!allKeywords.Any(x => message.Content.Contains(x.Keyword)))
                return;

            // It does. For any matched keywords, is the target user in the guild?
            foreach (var matchedKeyword in allKeywords.Where(x => message.Content.Contains(x.Keyword, StringComparison.OrdinalIgnoreCase)))
            {
                try
                {
                    await message.Channel.Guild.GetMemberAsync(matchedKeyword.UserId);
                }
                catch (NotFoundException)
                {
                    // User is not in guild. Skip.
                    return;
                }
            }

            // Yes. Continue with other checks.

            // Get message before current to check for assumed presence
            // Attempt to get message from cache before fetching from Discord to avoid potential API spam
            (ulong messageId, ulong authorId) msgBefore = default;
            if (Setup.State.Caches.MessageCache.TryGetMessageByChannel(message.Channel.Id, out var cachedMessage))
            {
                var messageId = cachedMessage.MessageId;
                var authorId = cachedMessage.AuthorId;
                if (messageId == message.Channel.LastMessageId)
                {
                    msgBefore = (messageId, authorId);
                }
            }

            if (msgBefore == default && !message.Channel.HasHighRatelimitRisk())
            {
                var msgsBefore = await message.Channel.GetMessagesBeforeAsync(message.Id, 1).ToListAsync();
                if (msgsBefore.Count > 0)
                    msgBefore = (msgsBefore[0].Id, msgsBefore[0].Author.Id);
            }

            foreach (var keyword in allKeywords)
            {
                // Checks

                // If keyword is set to only match whole word, use regex to check
                if (keyword.MatchWholeWord)
                {
                    if (!Regex.IsMatch(message.Content.ToLower().Replace("\n", " "), $@"\b{keyword.Keyword.Replace("\n", " ")}\b"))
                        continue;
                }
                // Otherwise, use a simple .Contains()
                else
                {
                    if (!message.Content.ToLower().Replace("\n", " ")
                            .Contains(keyword.Keyword.ToLower().Replace("\n", " "))) continue;
                }

                // Ignore messages sent by self
                if (message.Author.Id == keyword.UserId)
                    continue;

                // If message was sent by a user in the list of users to ignore for this keyword, ignore
                if (keyword.UserIgnoreList.Contains(message.Author.Id))
                    continue;

                // If message was sent in a channel in the list of channels to ignore for this keyword, ignore
                if (keyword.ChannelIgnoreList.Contains(message.Channel.Id))
                    continue;

                // If message was sent in a guild in the list of guilds to ignore for this keyword, ignore
                if (keyword.GuildIgnoreList.Contains(message.Channel.Guild.Id))
                    continue;

                // If message was sent by a bot and bots should be ignored for this keyword, ignore
                if (keyword.IgnoreBots && message.Author.IsBot)
                    continue;

                // If user is seemingly present and we should assume presence, ignore
                if (keyword.AssumePresence)
                    if (msgBefore != default && msgBefore.authorId == keyword.UserId)
                        continue;

                // If keyword is limited to a guild and this is not that guild, ignore
                if (keyword.GuildId != default && keyword.GuildId != message.Channel.Guild.Id)
                    continue;

                // Don't DM the user if their keyword was mentioned in a channel they do not have permissions to view
                DiscordMember member = default;
                try
                {
                    member = await message.Channel.Guild.GetMemberAsync(keyword.UserId); // need to fetch member to check permissions
                }
                catch (NotFoundException)
                {
                    // Member is not in server. We cannot check permissions, and it would be silly to continue anyway. Stop here.
                    return;
                }

                if (!message.Channel.PermissionsFor(member).HasPermission(DiscordPermission.ViewChannel) || !message.Channel.PermissionsFor(member).HasPermission(DiscordPermission.ReadMessageHistory))
                    break;

                await keyword.SendAlertMessageAsync(message, isEdit);
            }
        }
    }
}

namespace MechanicalMilkshake.Helpers;

public class KeywordTrackingHelpers
{
    public static async Task KeywordCheck(DiscordMessage message, bool isEdit = false)
    {
        if (message.Author is null || message.Content is null)
            return;

        if (message.Author.Id == Program.Discord.CurrentUser.Id)
            return;

        if (message.Channel.IsPrivate)
            return;

        if (isEdit && (message.EditedTimestamp is null || message.CreationTimestamp == message.EditedTimestamp))
            return;

        var fields = await Program.Db.HashGetAllAsync("keywords");
        var keywordsList = fields.Select(x => JsonConvert.DeserializeObject<TrackedKeyword>(x.Value)).ToList();
        
        // Stop! Before we make ANY API calls, does this message even contain any keywords?
        if (!keywordsList.Any(x => message.Content.Contains(x.Keyword)))
            return;
        
        // It does*. For any matched keywords, is the target user in the guild?
        // *this is a broad check, we check again more specifically later (respecting other keyword properties)
        foreach (var matchedKeyword in keywordsList.Where(x => message.Content.Contains(x.Keyword, StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                await message.Channel.Guild.GetMemberAsync(matchedKeyword.UserId);
            }
            catch
            {
                // User is not in guild. Skip.
                return;
            }
        }
        
        // Yes. Continue with other checks.
        
        // Get message before current to check for assumed presence
        // Attempt to get message from cache before fetching from Discord to avoid potential API spam
        // todo: can we do this later, but without spamming the API (like repeatedly checking the same message for different keywords in the foreach loop below)?
        (ulong messageId, ulong authorId) msgBefore = default;
        var msgFoundInCache = false;
        if (Program.MessageCache.TryGetMessageByChannel(message.Channel.Id, out var cachedMessage))
        {
            var messageId = cachedMessage.MessageId;
            var authorId = cachedMessage.AuthorId;
            if (messageId == message.Channel.LastMessageId)
            {
                msgBefore = (messageId, authorId);
                msgFoundInCache = true;
            }
        }

        if (!msgFoundInCache)
        {
            // Avoid fetching messages from channels that are known to be spammy / cause ratelimits
            if (Program.ConfigJson.Ids.RatelimitCautionChannels is not null && !Program.ConfigJson.Ids.RatelimitCautionChannels.Contains(message.Channel.Id.ToString()))
            {
                var msgsBefore = await message.Channel.GetMessagesBeforeAsync(message.Id, 1).ToListAsync();
                if (msgsBefore.Count > 0) msgBefore = (msgsBefore[0].Id, msgsBefore[0].Author.Id);
            }
        }

        foreach (var field in fields)
        {
            // Checks

            var fieldValue = JsonConvert.DeserializeObject<TrackedKeyword>(field.Value);

            // If keyword is set to only match whole word, use regex to check
            if (fieldValue!.MatchWholeWord)
            {
                if (!Regex.IsMatch(message.Content.ToLower().Replace("\n", " "),
                        $@"\b{field.Name.ToString().Replace("\n", " ")}\b")) continue;
            }
            // Otherwise, use a simple .Contains()
            else
            {
                if (!message.Content.ToLower().Replace("\n", " ")
                        .Contains(fieldValue.Keyword.ToLower().Replace("\n", " "))) continue;
            }

            // If message was sent by (this) bot, ignore
            if (message.Author.Id == Program.Discord.CurrentUser.Id)
                break;

            // Ignore messages sent by self
            if (message.Author.Id == fieldValue!.UserId)
                continue;

            // If message was sent by a user in the list of users to ignore for this keyword, ignore
            if (fieldValue.UserIgnoreList.Contains(message.Author.Id))
                continue;

            // If message was sent in a channel in the list of channels to ignore for this keyword, ignore
            if (fieldValue.ChannelIgnoreList.Contains(message.Channel.Id))
                continue;

            // If message was sent in a guild in the list of guilds to ignore for this keyword, ignore
            if (fieldValue.GuildIgnoreList.Contains(message.Channel.Guild.Id))
                continue;

            // If message was sent by a bot and bots should be ignored for this keyword, ignore
            if (fieldValue.IgnoreBots && message.Author.IsBot)
                continue;
            
            // If user is seemingly present and we should assume presence, ignore
            if (fieldValue.AssumePresence)
                if (msgBefore != default && msgBefore.authorId == fieldValue.UserId)
                    continue;

            // If keyword is limited to a guild and this is not that guild, ignore
            if (fieldValue.GuildId != default && fieldValue.GuildId != message.Channel.Guild.Id)
                continue;

            // Don't DM the user if their keyword was mentioned in a channel they do not have permissions to view.
            // If we don't do this we may leak private channels, which - even if the user might want to - I don't want to be doing.
            DiscordMember member = default;
            try
            {
                member = await message.Channel.Guild.GetMemberAsync(fieldValue.UserId); // need to fetch member to check permissions
            }
            catch (NotFoundException)
            {
                // Member is not in server. We cannot check permissions, and it would be silly to continue anyway. Stop here.
                return;
            }
            
            if (!message.Channel.PermissionsFor(member).HasPermission(DiscordPermissions.AccessChannels) || !message.Channel.PermissionsFor(member).HasPermission(DiscordPermissions.ReadMessageHistory))
                break;

            if (fieldValue.MatchWholeWord)
                await KeywordAlert(fieldValue.UserId, message, field.Name, isEdit);
            else
                await KeywordAlert(fieldValue.UserId, message, fieldValue.Keyword, isEdit);
        }
    }
    
    public static async Task<DiscordEmbedBuilder> GenerateKeywordDetailsEmbed(TrackedKeyword keyword)
    {
        var ignoredUserMentions = "\n";
        foreach (var userToIgnore in keyword!.UserIgnoreList)
        {
            var user = await Program.Discord.GetUserAsync(userToIgnore);
            ignoredUserMentions += $"- {user.Mention}\n";
        }

        if (ignoredUserMentions == "\n") ignoredUserMentions = " None\n";

        var ignoredChannelMentions = "\n";
        foreach (var channelToIgnore in keyword.ChannelIgnoreList)
        {
            var channel = await Program.Discord.GetChannelAsync(channelToIgnore);
            ignoredChannelMentions += $"- {channel.Mention}\n";
        }

        if (ignoredChannelMentions == "\n") ignoredChannelMentions = " None\n";

        var ignoredGuildNames = "\n";
        foreach (var guildToIgnore in keyword.GuildIgnoreList)
        {
            var guild = await Program.Discord.GetGuildAsync(guildToIgnore);
            ignoredGuildNames += $"- {guild.Name}\n";
        }

        if (ignoredGuildNames == "\n") ignoredGuildNames = " None\n";

        var matchWholeWord = keyword.MatchWholeWord.ToString().Trim();

        var limitedGuild = keyword.GuildId == default
            ? "None"
            : (await Program.Discord.GetGuildAsync(keyword.GuildId)).Name;

        DiscordEmbedBuilder embed = new()
        {
            Title = "Keyword Details",
            Color = Program.BotColor,
            Description = keyword.Keyword
        };

        embed.AddField("Ignored Users", ignoredUserMentions, true);
        embed.AddField("Ignored Channels", ignoredChannelMentions, true);
        embed.AddField("Ignored Servers", ignoredGuildNames, true);
        embed.AddField("Ignore Bots", keyword.IgnoreBots.ToString(), true);
        embed.AddField("Match Whole Word", matchWholeWord, true);
        embed.AddField("Assume Presence", keyword.AssumePresence.ToString(), true);
        embed.AddField("Limited to Server", limitedGuild, true);

        return embed;
    }

    private static async Task KeywordAlert(ulong targetUserId, DiscordMessage message, string keyword,
        bool isEdit = false)
    {
        DiscordMember member;
        try
        {
            member = await message.Channel.Guild.GetMemberAsync(targetUserId);
        }
        catch
        {
            // User is not in guild. Skip.
            return;
        }

        DiscordEmbedBuilder embed = new()
        {
            Color = new DiscordColor("#7287fd"),
            Title = keyword.Length > 225 ? "Tracked keyword triggered!" : $"Tracked keyword \"{keyword}\" triggered!",
            Description = message.Content
        };

        if (isEdit)
            embed.WithFooter("This alert was triggered by an edit to the message.");

        embed.AddField("Author ID", $"{message.Author.Id}", true);
        embed.AddField("Author Mention", $"{message.Author.Mention}", true);

        embed.AddField("Channel",
            $"{message.Channel.Mention} in {message.Channel.Guild.Name} | [Jump Link]({message.JumpLink})");

        try
        {
            await member.SendMessageAsync(embed);
        }
        catch
        {
            // User has DMs disabled.
        }
    }
    
}
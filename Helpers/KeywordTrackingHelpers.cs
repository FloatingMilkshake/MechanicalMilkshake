﻿namespace MechanicalMilkshake.Helpers;

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

        var fields = await Program.Db.HashGetAllAsync("keywords");
        
        // Get message before current to check for assumed presence
        // Attempt to get message from cache before fetching from Discord to avoid potential API spam
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
            var msgsBefore = await message.Channel.GetMessagesBeforeAsync(message.Id, 1);
            if (msgsBefore.Count > 0) msgBefore = (msgsBefore[0].Id, msgsBefore[0].Author.Id);
        }

        // Try to get member; if they are not in the guild, skip
        DiscordMember member;
        try
        {
            member = await message.Channel.Guild.GetMemberAsync(message.Author.Id);
        }
        catch
        {
            // User is not in guild. Skip.
            return;
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
            if (!message.Channel.PermissionsFor(member).HasPermission(Permissions.AccessChannels))
                break;

            if (fieldValue.MatchWholeWord)
                await KeywordAlert(fieldValue.UserId, message, field.Name, isEdit);
            else
                await KeywordAlert(fieldValue.UserId, message, fieldValue.Keyword, isEdit);
        }
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
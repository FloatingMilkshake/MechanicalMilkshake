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

        var fields = await Program.Db.HashGetAllAsync("keywords");

        foreach (var field in fields)
        {
            // Checks

            var fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);

            // If message was sent by (this) bot, ignore
            if (message.Author.Id == Program.Discord.CurrentUser.Id)
                break;

            // Ignore messages sent by self
            if (message.Author.Id == fieldValue!.UserId)
                continue;

            // If message was sent by a user in the list of users to ignore for this keyword, ignore
            if (fieldValue.IgnoreList.Contains(message.Author.Id))
                continue;

            // If message was sent by a bot and bots should be ignored for this keyword, ignore
            if (fieldValue.IgnoreBots && message.Author.IsBot)
                continue;

            // If keyword is limited to a guild and this is not that guild, ignore
            if (fieldValue.GuildId != default && fieldValue.GuildId != message.Channel.Guild.Id)
                continue;

            DiscordMember member;
            try
            {
                member = await message.Channel.Guild.GetMemberAsync(fieldValue.UserId);
            }
            catch
            {
                // User is not in guild. Skip.
                continue;
            }

            // Don't DM the user if their keyword was mentioned in a channel they do not have permissions to view.
            // If we don't do this we may leak private channels, which - even if the user might want to - I don't want to be doing.
            if (!message.Channel.PermissionsFor(member).HasPermission(Permissions.AccessChannels))
                break;

            // If keyword is set to only match whole word, use regex to check
            if (fieldValue.MatchWholeWord)
            {
                if (!Regex.IsMatch(message.Content.ToLower().Replace("\n", " "),
                        $"\\b{field.Name.ToString().Replace("\n", " ")}\\b")) continue;
                await KeywordAlert(fieldValue.UserId, message, field.Name, isEdit);
            }
            // Otherwise, use a simple .Contains()
            else
            {
                if (!message.Content.ToLower().Replace("\n", " ")
                        .Contains(fieldValue.Keyword.ToLower().Replace("\n", " "))) continue;
                await KeywordAlert(fieldValue.UserId, message, fieldValue.Keyword, isEdit);
            }
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
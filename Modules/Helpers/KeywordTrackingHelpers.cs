namespace MechanicalMilkshake.Modules.Helpers
{
    public class KeywordTrackingHelpers
    {
        public static async Task KeywordCheck(DiscordMessage message)
        {
            if (message.Author.Id == Program.discord.CurrentUser.Id)
                return;

            HashEntry[] fields = await Program.db.HashGetAllAsync("keywords");

            foreach (HashEntry field in fields)
            {
                // Checks

                KeywordConfig fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);

                // If message was sent by (this) bot, ignore
                if (message.Author.Id == Program.discord.CurrentUser.Id)
                    break;

                // Ignore messages sent by self
                if (message.Author.Id == fieldValue.UserId)
                    continue;

                // If message was sent by a user in the list of users to ignore for this keyword, ignore
                if (fieldValue.IgnoreList.Contains(message.Author.Id))
                    continue;

                // If message was sent by a bot and bots should be ignored for this keyword, ignore
                if (fieldValue.IgnoreBots == true && message.Author.IsBot)
                    continue;

                DiscordMember member;
                try
                {
                    member = await message.Channel.Guild.GetMemberAsync(fieldValue.UserId);
                }
                catch
                {
                    // User is not in guild. Skip.
                    break;
                }

                // Don't DM the user if their keyword was mentioned in a channel they do not have permissions to view.
                // If we don't do this we may leak private channels, which - even if the user might want to - I don't want to be doing.
                if (!message.Channel.PermissionsFor(member).HasPermission(Permissions.AccessChannels))
                    break;

                // If keyword is set to only match whole word, use regex to check
                if (fieldValue.MatchWholeWord)
                {
                    if (Regex.IsMatch(message.Content.ToLower(), $"\\b{field.Name}\\b"))
                    {
                        await KeywordAlert(fieldValue.UserId, message, field.Name);
                        return;
                    }
                }
                // Otherwise, use a simple .Contains()
                else
                {
                    if (message.Content.ToLower().Contains(fieldValue.Keyword))
                    {
                        await KeywordAlert(fieldValue.UserId, message, fieldValue.Keyword);
                        return;
                    }
                }
            }
        }

        public static async Task KeywordAlert(ulong targetUserId, DiscordMessage message, string keyword)
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
                Title = $"Tracked keyword \"{keyword}\" triggered!",
                Description = message.Content
            };

            embed.AddField("Author ID", $"{message.Author.Id}", true);
            embed.AddField("Author Mention", $"{message.Author.Mention}", true);

            if (message.Channel.IsPrivate)
                embed.AddField("Channel", $"Message sent in DMs.");
            else
                embed.AddField("Channel", $"{message.Channel.Mention} in {message.Channel.Guild.Name} | [Jump Link]({message.JumpLink})");

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
}

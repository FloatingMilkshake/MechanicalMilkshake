namespace MechanicalMilkshake.Modules
{
    internal class Helpers
    {
        public static string GetDebugInfo()
        {
            string commitHash = "";
            if (File.Exists("CommitHash.txt"))
            {
                StreamReader readHash = new("CommitHash.txt");
                commitHash = readHash.ReadToEnd().Trim();
            }
            if (commitHash == "")
            {
                commitHash = "dev";
            }

            string commitTime = "";
            string commitTimeDescription = "";
            if (File.Exists("CommitTime.txt"))
            {
                StreamReader readTime = new("CommitTime.txt");
                commitTime = readTime.ReadToEnd();
                commitTimeDescription = "Commit timestamp:";
            }
            if (commitTime == "")
            {
                commitTime = Program.connectTime.ToString();
                commitTimeDescription = "Last connected to Discord at";
            }

            string commitMessage = "";
            if (File.Exists("CommitMessage.txt"))
            {
                StreamReader readMessage = new("CommitMessage.txt");
                commitMessage = readMessage.ReadToEnd();
            }
            if (commitMessage == "")
            {
                commitMessage = $"Running in development mode; process started at {Program.processStartTime}";
            }

            return $"\nFramework: `{RuntimeInformation.FrameworkDescription}`"
                + $"\nPlatform: `{RuntimeInformation.OSDescription}`"
                + $"\nLibrary: `DSharpPlus {Program.discord.VersionString}`"
                + "\n"
                + $"\nLatest commit: `{commitHash}`"
                + $"\n{commitTimeDescription} `{commitTime}`"
                + $"\nLatest commit message:\n```\n{commitMessage}\n```";
        }

        public static string GetBadges(DiscordUser user)
        {
            string badges = "";
            if (user.Flags.Value.HasFlag(UserFlags.VerifiedBotDeveloper))
            {
                badges += $"<:earlyVerifiedBotDeveloper:{Program.userFlagEmoji.GetValueOrDefault("earlyVerifiedBotDeveloper")}> Early Verified Bot Developer\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.DiscordEmployee))
            {
                badges += $"<:discordStaff:{Program.userFlagEmoji.GetValueOrDefault("discordStaff")}> Discord Staff\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.HouseBalance))
            {
                badges += $"<:hypesquadBalance:{Program.userFlagEmoji.GetValueOrDefault("hypesquadBalance")}> HypeSquad Balance\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.HouseBravery))
            {
                badges += $"<:hypesquadBravery:{Program.userFlagEmoji.GetValueOrDefault("hypesquadBravery")}> HypeSquad Bravery\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.HouseBrilliance))
            {
                badges += $"<:hypesquadBrilliance:{Program.userFlagEmoji.GetValueOrDefault("hypesquadBrilliance")}> HypeSquad Brilliance\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.HypeSquadEvents))
            {
                badges += $"<:hypesquadEvents:{Program.userFlagEmoji.GetValueOrDefault("hypesquadEvents")}> HypeSquad Events\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.BugHunterLevelOne))
            {
                badges += $"<:bugHunterLevelOne:{Program.userFlagEmoji.GetValueOrDefault("bugHunterLevelOne")}> Bug Hunter Level One\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.BugHunterLevelTwo))
            {
                badges += $"<:bugHunterLevelTwo:{Program.userFlagEmoji.GetValueOrDefault("bugHunterLevelTwo")}> BugHunterLevelTwo\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.DiscordCertifiedModerator))
            {
                badges += $"<:certifiedModerator:{Program.userFlagEmoji.GetValueOrDefault("certifiedModerator")}> Discord Certified Moderator\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.DiscordPartner))
            {
                badges += $"<:partneredServerOwner:{Program.userFlagEmoji.GetValueOrDefault("partneredServerOwner")}> Partnered Server Owner\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.VerifiedBot))
            {
                badges += $"<:verifiedBot1:{Program.userFlagEmoji.GetValueOrDefault("verifiedBot1")}><:verifiedBot2:{Program.userFlagEmoji.GetValueOrDefault("verifiedBot2")}> Verified Bot\n";
            }
            if (user.Flags.Value.HasFlag(UserFlags.EarlySupporter))
            {
                badges += $"<:earlySupporter:{Program.userFlagEmoji.GetValueOrDefault("earlySupporter")}> Early Supporter\n";
            }

            return badges.Trim();
        }

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
                        await KeywordAlert(fieldValue.UserId, message, field.Name);
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

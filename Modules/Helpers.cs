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

            return badges.Trim();
        }

        public static async Task KeywordCheck(DiscordMessage message)
        {
            if (message.Author.Id == Program.discord.CurrentUser.Id)
                return;

            Owner.Private ownerPrivate = new();
#if DEBUG
            string keys = await ownerPrivate.RunCommand("redis-cli keys \\*");
#else
            string keys = await ownerPrivate.RunCommand("redis-cli -h redis keys \\*");
#endif
            keys = Regex.Replace(keys, @".*: ```", "").Replace("`", "");
            string[] splitKeys = keys.Replace("\r", "").Split("\n");
            List<ulong> userIds = new();
            foreach (string key in splitKeys)
            {
                if (key == "")
                    continue;
                userIds.Add(Convert.ToUInt64(key));
            }

            foreach (ulong id in userIds)
            {
                var data = await Program.db.HashGetAllAsync(id.ToString());
                foreach (var field in data)
                {
                    if (message.Author == await Program.discord.GetUserAsync(id))
                        break;

                    KeywordConfig fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);
                    if (fieldValue.IgnoreList.Contains(message.Author.Id))
                        continue;

                    if (fieldValue.IgnoreBots == true && message.Author.IsBot)
                        continue;

                    DiscordMember member;
                    try
                    {
                        member = await message.Channel.Guild.GetMemberAsync(id);
                    }
                    catch
                    {
                        // User is not in guild. Skip.
                        break;
                    }
                    if (!message.Channel.PermissionsFor(member).HasPermission(Permissions.AccessChannels))
                        break;

                    if (message.Content.ToLower().Contains(field.Name))
                    {
                        await KeywordAlert(id, message, field.Name);
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

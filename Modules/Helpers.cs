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

        public static async Task KeywordCheck(DiscordMessage message)
        {
            List<ulong> excludedIds = new()
            {
                Program.discord.CurrentUser.Id,
                455432936339144705,
                849370001009016852
            };

            if (excludedIds.Contains(message.Author.Id))
                return;

            if (message.Content.Contains("floaty"))
                await SendAlert("floaty", message);
            else if (message.Content.Contains("milkshake"))
                await SendAlert("milkshake", message);
            else if (message.Content.Contains("455432936339144705"))
                await SendAlert("455432936339144705", message);


            static async Task SendAlert(string keyword, DiscordMessage message)
            {
                DiscordGuild guild = await Program.discord.GetGuildAsync(799644062973427743);
                DiscordMember member = await guild.GetMemberAsync(455432936339144705);

                DiscordEmbedBuilder embed = new()
                {
                    Color = new DiscordColor("#7287fd"),
                    Title = $"Tracked keyword \"{keyword}\" triggered!",
                    Description = $"{message.Content}"
                };
                embed.AddField("Author ID", $"{message.Author.Id}", true);
                embed.AddField("Author Mention", $"{message.Author.Mention}", true);

                if (message.Channel.IsPrivate)
                    embed.AddField("Channel", $"Message sent in DMs.");
                else
                    embed.AddField("Channel", $"{message.Channel.Mention} in {message.Channel.Guild.Name} | [Jump Link]({message.JumpLink})");

                await member.SendMessageAsync(embed);
            }
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
    }
}

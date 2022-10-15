namespace MechanicalMilkshake.Helpers
{
    public class UserInfoHelpers
    {
        // Generate embed with extended info when provided with a DiscordMember
        public static async Task<DiscordEmbed> GenerateUserInfoEmbed(DiscordMember member)
        {
            var msSinceEpoch = member.Id >> 22;
            var msUnix = msSinceEpoch + 1420070400000;
            var registeredAt = $"{msUnix / 1000}";

            var t = member.JoinedAt - new DateTime(1970, 1, 1);
            var joinedAtTimestamp = (int)t.TotalSeconds;

            string acknowledgements = null;
            if (member.Permissions.HasPermission(Permissions.KickMembers) &&
                member.Permissions.HasPermission(Permissions.BanMembers))
                acknowledgements = "Server Moderator (can kick and ban members)";

            if (member.Permissions.HasPermission(Permissions.Administrator)) acknowledgements = "Server Administrator";

            if (member.IsOwner) acknowledgements = "Server Owner";

            var roles = "None";
            if (member.Roles.Any())
            {
                if (member.Roles.Count() > 30)
                {
                    roles = "";
                    var count = 0;
                    foreach (var role in member.Roles.OrderBy(role => role.Position).Reverse())
                        if (count < 30)
                        {
                            roles += role.Mention + " ";
                            count++;
                        }

                    roles += "\n*Only the highest 30 roles are displayed here... why so many?*";
                }
                else
                {
                    roles = "";
                    foreach (var role in member.Roles.OrderBy(role => role.Position).Reverse()) roles += role.Mention + " ";
                }
            }

            var extendedUserInfoEmbed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor($"{member.Color}"))
                .AddField("User Mention", member.Mention)
                .AddField("User ID", $"{member.Id}");

            if (member.Nickname is not null)
                extendedUserInfoEmbed.AddField("Nickname", member.Nickname);

            extendedUserInfoEmbed.AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)");
            extendedUserInfoEmbed.AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)");
            extendedUserInfoEmbed.AddField($"Roles - {member.Roles.Count()}", roles);
            extendedUserInfoEmbed.WithThumbnail(member.AvatarUrl);

            if (acknowledgements != null) extendedUserInfoEmbed.AddField("Acknowledgements", acknowledgements);

            if (member.PremiumSince != null)
            {
                var PremiumSinceUtc = member.PremiumSince.Value.UtcDateTime;
                var unixTime = ((DateTimeOffset)PremiumSinceUtc).ToUnixTimeSeconds();
                var boostingSince = $"Boosting since <t:{unixTime}:R> (<t:{unixTime}:F>";

                extendedUserInfoEmbed.AddField("Server Booster", boostingSince);
            }

            var badges = GetBadges(member);
            if (badges != "") extendedUserInfoEmbed.AddField("Badges", badges);

            return extendedUserInfoEmbed;
        }

        // Generate embed with limited info when provided with a DiscordUser
        public static async Task<DiscordEmbed> GenerateUserInfoEmbed(DiscordUser user)
        {
            var createdAt = IdHelpers.GetCreationTimestamp(user.Id);

            var basicUserInfoEmbed = new DiscordEmbedBuilder()
                .WithThumbnail($"{user.AvatarUrl}")
                .WithColor(Program.botColor)
                .AddField("ID", $"{user.Id}")
                .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

            var userBadges = GetBadges(user);
            if (userBadges != "") basicUserInfoEmbed.AddField("Badges", userBadges);

            return basicUserInfoEmbed;
        }

        private static string GetBadges(DiscordUser user)
        {
            var badges = "";
            if (user.Flags.Value.HasFlag(UserFlags.VerifiedBotDeveloper))
                badges +=
                    $"<:earlyVerifiedBotDeveloper:{Program.userFlagEmoji.GetValueOrDefault("earlyVerifiedBotDeveloper")}> Early Verified Bot Developer\n";

            if (user.Flags.Value.HasFlag(UserFlags.DiscordEmployee))
                badges += $"<:discordStaff:{Program.userFlagEmoji.GetValueOrDefault("discordStaff")}> Discord Staff\n";

            if (user.Flags.Value.HasFlag(UserFlags.HouseBalance))
                badges +=
                    $"<:hypesquadBalance:{Program.userFlagEmoji.GetValueOrDefault("hypesquadBalance")}> HypeSquad Balance\n";

            if (user.Flags.Value.HasFlag(UserFlags.HouseBravery))
                badges +=
                    $"<:hypesquadBravery:{Program.userFlagEmoji.GetValueOrDefault("hypesquadBravery")}> HypeSquad Bravery\n";

            if (user.Flags.Value.HasFlag(UserFlags.HouseBrilliance))
                badges +=
                    $"<:hypesquadBrilliance:{Program.userFlagEmoji.GetValueOrDefault("hypesquadBrilliance")}> HypeSquad Brilliance\n";

            if (user.Flags.Value.HasFlag(UserFlags.HypeSquadEvents))
                badges +=
                    $"<:hypesquadEvents:{Program.userFlagEmoji.GetValueOrDefault("hypesquadEvents")}> HypeSquad Events\n";

            if (user.Flags.Value.HasFlag(UserFlags.BugHunterLevelOne))
                badges +=
                    $"<:bugHunterLevelOne:{Program.userFlagEmoji.GetValueOrDefault("bugHunterLevelOne")}> Bug Hunter Level One\n";

            if (user.Flags.Value.HasFlag(UserFlags.BugHunterLevelTwo))
                badges +=
                    $"<:bugHunterLevelTwo:{Program.userFlagEmoji.GetValueOrDefault("bugHunterLevelTwo")}> Bug Hunter Level Two\n";

            if (user.Flags.Value.HasFlag(UserFlags.DiscordCertifiedModerator))
                badges +=
                    $"<:certifiedModerator:{Program.userFlagEmoji.GetValueOrDefault("certifiedModerator")}> Discord Certified Moderator\n";

            if (user.Flags.Value.HasFlag(UserFlags.DiscordPartner))
                badges +=
                    $"<:partneredServerOwner:{Program.userFlagEmoji.GetValueOrDefault("partneredServerOwner")}> Partnered Server Owner\n";

            if (user.Flags.Value.HasFlag(UserFlags.VerifiedBot))
                badges +=
                    $"<:verifiedBot1:{Program.userFlagEmoji.GetValueOrDefault("verifiedBot1")}><:verifiedBot2:{Program.userFlagEmoji.GetValueOrDefault("verifiedBot2")}> Verified Bot\n";

            if (user.Flags.Value.HasFlag(UserFlags.EarlySupporter))
                badges +=
                    $"<:earlySupporter:{Program.userFlagEmoji.GetValueOrDefault("earlySupporter")}> Early Supporter\n";

            return badges.Trim();
        }
    }
}

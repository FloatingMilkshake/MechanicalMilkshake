namespace MechanicalMilkshake.Extensions;

internal static class DiscordMemberExtensions
{
    extension(DiscordMember member)
    {
        internal bool CanModerate(DiscordMember targetMember)
        {
            if (member == default)
                return false;

            return targetMember == default || member.Hierarchy > targetMember.Hierarchy;
        }

        internal DiscordEmbed CreateUserInfoEmbed()
        {
            var registeredAt = $"{member.Id.ToUnixTimeSeconds()}";

            var joinedAtTimestamp = member.JoinedAt.DateTime.ToUnixTimeSeconds();

            List<string> notablePerms = [];
            if (member.IsOwner)
            {
                notablePerms.Add("Server Owner");
            }
            else if (member.Permissions.HasPermission(DiscordPermission.Administrator))
            {
                notablePerms.Add("Administrator");
            }
            else
            {
                if (member.Permissions.HasPermission(DiscordPermission.BanMembers))
                    notablePerms.Add("Ban Members");
                if (member.Permissions.HasPermission(DiscordPermission.KickMembers))
                    notablePerms.Add("Kick, Approve, and Reject Members");
                if (member.Permissions.HasPermission(DiscordPermission.ModerateMembers))
                    notablePerms.Add("Timeout Members");
                if (member.Permissions.HasPermission(DiscordPermission.ManageGuild))
                    notablePerms.Add("Manage Server");
                if (member.Permissions.HasPermission(DiscordPermission.ManageRoles))
                    notablePerms.Add("Manage Roles");
                if (member.Permissions.HasPermission(DiscordPermission.ManageChannels))
                    notablePerms.Add("Manage Channels");
                if (member.Permissions.HasPermission(DiscordPermission.MentionEveryone))
                    notablePerms.Add("Mention @everyone, @here, and All Roles");
                if (member.Permissions.HasPermission(DiscordPermission.ManageGuildExpressions))
                    notablePerms.Add("Manage Expressions");
                if (member.Permissions.HasPermission(DiscordPermission.CreateGuildExpressions))
                    notablePerms.Add("Create Expressions");
                if (member.Permissions.HasPermission(DiscordPermission.ManageEvents))
                    notablePerms.Add("Manage Events");
                if (member.Permissions.HasPermission(DiscordPermission.ManageMessages))
                    notablePerms.Add("Manage Messages");
                if (member.Permissions.HasPermission(DiscordPermission.PinMessages))
                    notablePerms.Add("Pin Messages");
                if (member.Permissions.HasPermission(DiscordPermission.BypassSlowmode))
                    notablePerms.Add("Bypass Slowmode");
                if (member.Permissions.HasPermission(DiscordPermission.ManageNicknames))
                    notablePerms.Add("Manage Nicknames");
                if (member.Permissions.HasPermission(DiscordPermission.ManageThreads))
                    notablePerms.Add(member.Guild.Features.Contains("COMMUNITY")
                        ? "Manage Threads and Posts"
                        : "Manage Threads");
                if (member.Permissions.HasPermission(DiscordPermission.ManageWebhooks))
                    notablePerms.Add("Manage Webhooks");
                if (member.Permissions.HasPermission(DiscordPermission.ViewAuditLog))
                    notablePerms.Add("View Audit Log");
                if (member.Permissions.HasPermission(DiscordPermission.ViewGuildInsights))
                    notablePerms.Add("View Server Insights");
                if (member.Permissions.HasPermission(DiscordPermission.PrioritySpeaker))
                    notablePerms.Add("Priority Speaker");
                if (member.Permissions.HasPermission(DiscordPermission.MuteMembers))
                    notablePerms.Add("Mute Members");
                if (member.Permissions.HasPermission(DiscordPermission.DeafenMembers))
                    notablePerms.Add("Deafen Members");
                if (member.Permissions.HasPermission(DiscordPermission.MoveMembers))
                    notablePerms.Add("Move Members");
            }

            var roles = "None";
            if (member.Roles.Any())
            {
                if (member.Roles.Count() > 30)
                {
                    roles = "";
                    var count = 0;
                    foreach (var role in member.Roles.OrderByDescending(role => role.Position))
                        if (count < 30)
                        {
                            roles += role.Mention + " ";
                            count++;
                        }

                    roles += "\n*Only the highest 30 roles are displayed here... why so many?*";
                }
                else
                {
                    roles = string.Join(" ", member.Roles.OrderByDescending(r => r.Position).Select(r => r.Mention));
                }
            }

            var rolesFieldName = "Roles";
            if (member.Roles.Any()) rolesFieldName += $" - {member.Roles.Count()}";

            var extendedUserInfoEmbed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor($"{member.Color.PrimaryColor}"))
                .AddField("User Mention", member.Mention, true)
                .WithFooter($"User ID: {member.Id}");

            if (member.GlobalName is not null)
                extendedUserInfoEmbed.AddField("Display Name", member.GlobalName, true);

            if (!string.IsNullOrWhiteSpace(member.Nickname))
                extendedUserInfoEmbed.AddField("Nickname", member.Nickname, true);

            extendedUserInfoEmbed.AddField("Account created on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)");
            extendedUserInfoEmbed.AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)");
            extendedUserInfoEmbed.AddField(rolesFieldName, roles);
            extendedUserInfoEmbed.WithThumbnail(member.DisplayAvatarUrl);

            var badges = member.GetBadges();
            if (badges != "")
                extendedUserInfoEmbed.AddField("Badges", badges, true);

            if (member.PremiumSince is not null)
            {
                var premiumSinceTimestamp = member.PremiumSince.Value.UtcDateTime.ToUnixTimeSeconds();
                var boostingSince = $"Boosting since <t:{premiumSinceTimestamp}:R> (<t:{premiumSinceTimestamp}:F>";

                extendedUserInfoEmbed.AddField("Server Booster", boostingSince, true);
            }

            if (notablePerms.Count > 0)
                extendedUserInfoEmbed.AddField("Notable Permissions",
                    string.Join("\n", notablePerms.Select(p => p)));

            return extendedUserInfoEmbed;
        }
    }
}

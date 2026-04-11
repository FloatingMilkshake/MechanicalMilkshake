namespace MechanicalMilkshake.Helpers;

internal class UserInfoHelpers
{
    // Generate embed with extended info when provided with a DiscordMember
    internal static Task<DiscordEmbed> GenerateUserInfoEmbed(DiscordMember member)
    {
        var registeredAt = $"{DateHelpers.GetUnixTimestamp(member.Id)}";

        var joinedAtTimestamp = DateHelpers.GetUnixTimestamp(member.JoinedAt.DateTime);

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

        var badges = GetBadges(member);
        if (badges != "") extendedUserInfoEmbed.AddField("Badges", badges, true);

        if (member.PremiumSince is not null)
        {
            var premiumSinceTimestamp = DateHelpers.GetUnixTimestamp(member.PremiumSince.Value.UtcDateTime);
            var boostingSince = $"Boosting since <t:{premiumSinceTimestamp}:R> (<t:{premiumSinceTimestamp}:F>";

            extendedUserInfoEmbed.AddField("Server Booster", boostingSince, true);
        }

        if (notablePerms.Count > 0)
            extendedUserInfoEmbed.AddField("Notable Permissions",
                string.Join("\n", notablePerms.Select(p => p)));

        return Task.FromResult<DiscordEmbed>(extendedUserInfoEmbed);
    }

    // Generate embed with limited info when provided with a DiscordUser
    internal static Task<DiscordEmbed> GenerateUserInfoEmbed(DiscordUser user)
    {
        var createdAt = DateHelpers.GetUnixTimestamp(user.Id);

        var basicUserInfoEmbed = new DiscordEmbedBuilder()
            .WithThumbnail($"{user.AvatarUrl}")
            .WithColor(Setup.Constants.BotColor)
            .WithFooter($"User ID: {user.Id}")
            .AddField("User Mention", user.Mention, true);
        if (user.GlobalName is not null) basicUserInfoEmbed.AddField("Display Name", user.GlobalName, true);
        basicUserInfoEmbed.AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

        var userBadges = GetBadges(user);
        if (userBadges != "") basicUserInfoEmbed.AddField("Badges", userBadges);

        return Task.FromResult<DiscordEmbed>(basicUserInfoEmbed);
    }

    // Get a user's discriminator, or return an empty string if they have the new username style (discrim == #0)
    internal static string GetDiscriminator(DiscordUser user)
    {
        return user.Discriminator == "0" ? "" : $"#{user.Discriminator}";
    }

    // Get a user's full username, including discriminator if applicable
    internal static string GetFullUsername(DiscordUser user)
    {
        return $"{user.Username + GetDiscriminator(user)}";
    }

    private static string GetBadges(DiscordUser user)
    {
        var badges = "";

        if (user.Flags is null) return "";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.VerifiedBotDeveloper))
            badges +=
                $"<:earlyVerifiedBotDeveloper:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("earlyVerifiedBotDeveloper")}> Early Verified Bot Developer\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.DiscordEmployee))
            badges += $"<:discordStaff:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("discordStaff")}> Discord Staff\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.HouseBalance))
            badges +=
                $"<:hypesquadBalance:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("hypesquadBalance")}> HypeSquad Balance\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.HouseBravery))
            badges +=
                $"<:hypesquadBravery:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("hypesquadBravery")}> HypeSquad Bravery\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.HouseBrilliance))
            badges +=
                $"<:hypesquadBrilliance:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("hypesquadBrilliance")}> HypeSquad Brilliance\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.HypeSquadEvents))
            badges +=
                $"<:hypesquadEvents:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("hypesquadEvents")}> HypeSquad Events\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.BugHunterLevelOne))
            badges +=
                $"<:bugHunterLevelOne:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("bugHunterLevelOne")}> Bug Hunter Level One\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.BugHunterLevelTwo))
            badges +=
                $"<:bugHunterLevelTwo:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("bugHunterLevelTwo")}> Bug Hunter Level Two\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.DiscordCertifiedModerator))
            badges +=
                $"<:certifiedModerator:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("certifiedModerator")}> Discord Certified Moderator\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.DiscordPartner))
            badges +=
                $"<:partneredServerOwner:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("partneredServerOwner")}> Partnered Server Owner\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.VerifiedBot))
            badges +=
                $"<:verifiedBot1:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("verifiedBot1")}><:verifiedBot2:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("verifiedBot2")}> Verified Bot\n";

        if (user.Flags.Value.HasFlag(DiscordUserFlags.EarlySupporter))
            badges +=
                $"<:earlySupporter:{Setup.Constants.UserFlagEmoji.GetValueOrDefault("earlySupporter")}> Early Supporter\n";


        return badges.Trim();
    }
}

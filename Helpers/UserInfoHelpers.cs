namespace MechanicalMilkshake.Helpers;

public class UserInfoHelpers
{
    // Generate embed with extended info when provided with a DiscordMember
    public static Task<DiscordEmbed> GenerateUserInfoEmbed(DiscordMember member)
    {
        var registeredAt = $"{IdHelpers.GetCreationTimestamp(member.Id, true)}";

        var t = member.JoinedAt - new DateTime(1970, 1, 1);
        var joinedAtTimestamp = (int)t.TotalSeconds;

        List<string> notablePerms = new();
        if (member.IsOwner)
        {
            notablePerms.Add("Server Owner");
        }
        else if (member.Permissions.HasPermission(Permissions.Administrator))
        {
            notablePerms.Add("Administrator");
        }
        else
        {
            if (member.Permissions.HasPermission(Permissions.ManageChannels))
                notablePerms.Add("Manage Channels");
            if (member.Permissions.HasPermission(Permissions.ManageEmojis))
                notablePerms.Add("Manage Emojis and Stickers");
            if (member.Permissions.HasPermission(Permissions.ManageEvents))
                notablePerms.Add("Manage Events");
            if (member.Permissions.HasPermission(Permissions.ManageGuild))
                notablePerms.Add("Manage Server");
            if (member.Permissions.HasPermission(Permissions.ManageMessages))
                notablePerms.Add("Manage Messages");
            if (member.Permissions.HasPermission(Permissions.ManageNicknames))
                notablePerms.Add("Manage Nicknames");
            if (member.Permissions.HasPermission(Permissions.ManageRoles))
                notablePerms.Add("Manage Roles");
            if (member.Permissions.HasPermission(Permissions.ManageThreads))
                notablePerms.Add(member.Guild.Features.Contains("COMMUNITY")
                    ? "Manage Threads and Posts"
                    : "Manage Threads");
            if (member.Permissions.HasPermission(Permissions.ManageWebhooks))
                notablePerms.Add("Manage Webhooks");
        }

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
                roles = member.Roles.OrderBy(role => role.Position).Reverse()
                    .Aggregate("", (current, role) => current + role.Mention + " ");
            }
        }

        var rolesFieldName = "Roles";
        if (member.Roles.Any()) rolesFieldName += $" - {member.Roles.Count()}";

        var extendedUserInfoEmbed = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor($"{member.Color}"))
            .AddField("User Mention", member.Mention, true)
            .WithFooter($"User ID: {member.Id}");
        
        if (member.Nickname is not null)
            extendedUserInfoEmbed.AddField("Nickname", member.Nickname, true);

        extendedUserInfoEmbed.AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)");
        extendedUserInfoEmbed.AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)");
        extendedUserInfoEmbed.AddField(rolesFieldName, roles);
        extendedUserInfoEmbed.WithThumbnail(member.AvatarUrl);
        
        var badges = GetBadges(member);
        if (badges != "") extendedUserInfoEmbed.AddField("Badges", badges, true);
        
        if (member.PremiumSince is not null)
        {
            var premiumSinceUtc = member.PremiumSince.Value.UtcDateTime;
            var unixTime = ((DateTimeOffset)premiumSinceUtc).ToUnixTimeSeconds();
            var boostingSince = $"Boosting since <t:{unixTime}:R> (<t:{unixTime}:F>";

            extendedUserInfoEmbed.AddField("Server Booster", boostingSince, true);
        }

        if (notablePerms.Count > 0)
            extendedUserInfoEmbed.AddField("Notable Permissions",
                notablePerms.Aggregate("", (current, perm) => current + $"{perm}\n"));

        return Task.FromResult<DiscordEmbed>(extendedUserInfoEmbed);
    }

    // Generate embed with limited info when provided with a DiscordUser
    public static Task<DiscordEmbed> GenerateUserInfoEmbed(DiscordUser user)
    {
        var createdAt = IdHelpers.GetCreationTimestamp(user.Id, true);

        var basicUserInfoEmbed = new DiscordEmbedBuilder()
            .WithThumbnail($"{user.AvatarUrl}")
            .WithColor(Program.BotColor)
            .AddField("ID", $"{user.Id}")
            .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

        var userBadges = GetBadges(user);
        if (userBadges != "") basicUserInfoEmbed.AddField("Badges", userBadges);

        return Task.FromResult<DiscordEmbed>(basicUserInfoEmbed);
    }

    private static string GetBadges(DiscordUser user)
    {
        var badges = "";

        if (user.Flags is null) return "";

        if (user.Flags.Value.HasFlag(UserFlags.VerifiedBotDeveloper))
            badges +=
                $"<:earlyVerifiedBotDeveloper:{Program.UserFlagEmoji.GetValueOrDefault("earlyVerifiedBotDeveloper")}> Early Verified Bot Developer\n";

        if (user.Flags.Value.HasFlag(UserFlags.DiscordEmployee))
            badges += $"<:discordStaff:{Program.UserFlagEmoji.GetValueOrDefault("discordStaff")}> Discord Staff\n";

        if (user.Flags.Value.HasFlag(UserFlags.HouseBalance))
            badges +=
                $"<:hypesquadBalance:{Program.UserFlagEmoji.GetValueOrDefault("hypesquadBalance")}> HypeSquad Balance\n";

        if (user.Flags.Value.HasFlag(UserFlags.HouseBravery))
            badges +=
                $"<:hypesquadBravery:{Program.UserFlagEmoji.GetValueOrDefault("hypesquadBravery")}> HypeSquad Bravery\n";

        if (user.Flags.Value.HasFlag(UserFlags.HouseBrilliance))
            badges +=
                $"<:hypesquadBrilliance:{Program.UserFlagEmoji.GetValueOrDefault("hypesquadBrilliance")}> HypeSquad Brilliance\n";

        if (user.Flags.Value.HasFlag(UserFlags.HypeSquadEvents))
            badges +=
                $"<:hypesquadEvents:{Program.UserFlagEmoji.GetValueOrDefault("hypesquadEvents")}> HypeSquad Events\n";

        if (user.Flags.Value.HasFlag(UserFlags.BugHunterLevelOne))
            badges +=
                $"<:bugHunterLevelOne:{Program.UserFlagEmoji.GetValueOrDefault("bugHunterLevelOne")}> Bug Hunter Level One\n";

        if (user.Flags.Value.HasFlag(UserFlags.BugHunterLevelTwo))
            badges +=
                $"<:bugHunterLevelTwo:{Program.UserFlagEmoji.GetValueOrDefault("bugHunterLevelTwo")}> Bug Hunter Level Two\n";

        if (user.Flags.Value.HasFlag(UserFlags.DiscordCertifiedModerator))
            badges +=
                $"<:certifiedModerator:{Program.UserFlagEmoji.GetValueOrDefault("certifiedModerator")}> Discord Certified Moderator\n";

        if (user.Flags.Value.HasFlag(UserFlags.DiscordPartner))
            badges +=
                $"<:partneredServerOwner:{Program.UserFlagEmoji.GetValueOrDefault("partneredServerOwner")}> Partnered Server Owner\n";

        if (user.Flags.Value.HasFlag(UserFlags.VerifiedBot))
            badges +=
                $"<:verifiedBot1:{Program.UserFlagEmoji.GetValueOrDefault("verifiedBot1")}><:verifiedBot2:{Program.UserFlagEmoji.GetValueOrDefault("verifiedBot2")}> Verified Bot\n";

        if (user.Flags.Value.HasFlag(UserFlags.EarlySupporter))
            badges +=
                $"<:earlySupporter:{Program.UserFlagEmoji.GetValueOrDefault("earlySupporter")}> Early Supporter\n";


        return badges.Trim();
    }
}

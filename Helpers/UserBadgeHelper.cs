namespace MechanicalMilkshake.Helpers;

public class UserBadgeHelper
{
    public static string GetBadges(DiscordUser user)
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
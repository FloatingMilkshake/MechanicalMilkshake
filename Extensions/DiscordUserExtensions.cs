namespace MechanicalMilkshake.Extensions;

internal static class DiscordUserExtensions
{
    extension(DiscordUser user)
    {
        // Get a user's discriminator, or return an empty string if they have the new username style (discrim == #0)
        internal string GetDiscriminator()
        {
            return user.Discriminator == "0" ? "" : $"#{user.Discriminator}";
        }

        // Get a user's full username, including discriminator if applicable
        internal string GetFullUsername()
        {
            return $"{user.Username + GetDiscriminator(user)}";
        }

        internal string GetBadges()
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

        internal DiscordEmbed CreateUserInfoEmbed()
        {
            var createdAt = user.Id.ToUnixTimeSeconds();

            var basicUserInfoEmbed = new DiscordEmbedBuilder()
                .WithThumbnail($"{user.AvatarUrl}")
                .WithColor(Setup.Constants.BotColor)
                .WithFooter($"User ID: {user.Id}")
                .AddField("User Mention", user.Mention, true);
            if (user.GlobalName is not null) basicUserInfoEmbed.AddField("Display Name", user.GlobalName, true);
            basicUserInfoEmbed.AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

            var userBadges = user.GetBadges();
            if (userBadges != "") basicUserInfoEmbed.AddField("Badges", userBadges);

            return basicUserInfoEmbed;
        }
    }
}

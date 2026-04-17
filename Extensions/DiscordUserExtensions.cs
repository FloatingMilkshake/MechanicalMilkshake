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

            if (user.Flags is null)
                return string.Empty;

            if (user.Flags.Value.HasFlag(DiscordUserFlags.VerifiedBotDeveloper))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "earlyVerifiedBotDeveloper");
                badges += $"<:{emoji.Name}:{emoji.Id}> Early Verified Bot Developer\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.DiscordEmployee))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "discordStaff");
                badges += $"<:{emoji.Name}:{emoji.Id}> Discord Staff\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.HouseBalance))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "hypesquadBalance");
                badges += $"<:{emoji.Name}:{emoji.Id}> HypeSquad Balance\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.HouseBravery))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "hypesquadBravery");
                badges += $"<:{emoji.Name}:{emoji.Id}> HypeSquad Bravery\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.HouseBrilliance))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "hypesquadBrilliance");
                badges += $"<:{emoji.Name}:{emoji.Id}> HypeSquad Brilliance\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.HypeSquadEvents))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "hypesquadEvents");
                badges += $"<:{emoji.Name}:{emoji.Id}> HypeSquad Events\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.BugHunterLevelOne))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "bugHunterLevelOne");
                badges += $"<:{emoji.Name}:{emoji.Id}> Bug Hunter Level One\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.BugHunterLevelTwo))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "bugHunterLevelTwo");
                badges += $"<:{emoji.Name}:{emoji.Id}> Bug Hunter Level Two\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.DiscordCertifiedModerator))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "certifiedModerator");
                badges += $"<:{emoji.Name}:{emoji.Id}> Discord Certified Moderator\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.DiscordPartner))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "partneredServerOwner");
                badges += $"<:{emoji.Name}:{emoji.Id}> Partnered Server Owner\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.VerifiedBot))
            {
                var emoji1 = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "verifiedBot1");
                var emoji2 = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "verifiedBot2");
                badges += $"<:{emoji1.Name}:{emoji1.Id}><:{emoji2.Name}:{emoji2.Id}> Verified Bot\n";
            }

            if (user.Flags.Value.HasFlag(DiscordUserFlags.EarlySupporter))
            {
                var emoji = Setup.State.Discord.ApplicationEmoji.First(e => e.Name == "earlySupporter");
                badges += $"<:{emoji.Name}:{emoji.Id}> Early Supporter\n";
            }

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

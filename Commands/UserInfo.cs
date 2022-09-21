namespace MechanicalMilkshake.Commands;

public class UserInfo : ApplicationCommandModule
{
    [SlashCommand("userinfo", "Returns information about the provided server member.")]
    [SlashRequireGuild]
    public async Task UserInfoCommand(InteractionContext ctx,
        [Option("member", "The member to look up information for. Defaults to yourself if no member is provided.")]
        DiscordUser user = null)
    {
        DiscordMember member = null;

        if (user != null)
            try
            {
                member = await ctx.Guild.GetMemberAsync(user.Id);
            }
            catch
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    "Hmm. It doesn't look like that user is in the server, so I can't fetch their user info."));
                return;
            }
        else
            user = ctx.User;

        if (member == null) member = ctx.Member;

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
                int count = 0;
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

        var embed = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor($"{member.Color}"))
            .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
            .AddField("User Mention", member.Mention)
            .AddField("User ID", $"{member.Id}");

        if (member.Nickname is not null)
            embed.AddField("Nickname", member.Nickname);

        embed.AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)");
        embed.AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)");
        embed.AddField($"Roles - {member.Roles.Count()}", roles);
        embed.WithThumbnail(member.AvatarUrl);
        embed.WithTimestamp(DateTime.UtcNow);

        if (acknowledgements != null) embed.AddField("Acknowledgements", acknowledgements);

        if (member.PremiumSince != null)
        {
            var PremiumSinceUtc = member.PremiumSince.Value.UtcDateTime;
            var unixTime = ((DateTimeOffset)PremiumSinceUtc).ToUnixTimeSeconds();
            var boostingSince = $"Boosting since <t:{unixTime}:R> (<t:{unixTime}:F>";

            embed.AddField("Server Booster", boostingSince);
        }

        var badges = UserBadgeHelper.GetBadges(user);
        if (badges != "") embed.AddField("Badges", badges);

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{member.Username}#{member.Discriminator}**").AddEmbed(embed));
    }
}
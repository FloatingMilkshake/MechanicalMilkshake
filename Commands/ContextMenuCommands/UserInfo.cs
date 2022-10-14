namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class UserInfo : ApplicationCommandModule
{
    [ContextMenu(ApplicationCommandType.UserContextMenu, "User Info")]
    [SlashRequireGuild]
    public async Task ContextUserInfo(ContextMenuContext ctx)
    {
        DiscordMember member = null;

        try
        {
            member = await ctx.Guild.GetMemberAsync(ctx.TargetUser.Id);
        }
        catch (NotFoundException)
        {
            var createdAt = (((ctx.TargetUser.Id >> 22) + 1420070400000) / 1000).ToString();

            var basicUserInfoEmbed = new DiscordEmbedBuilder()
                .WithThumbnail($"{ctx.TargetUser.AvatarUrl}")
                .WithColor(Program.botColor)
                .AddField("ID", $"{ctx.TargetUser.Id}")
                .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

            var userBadges = UserBadgeHelper.GetBadges(ctx.TargetUser);
            if (userBadges != "") basicUserInfoEmbed.AddField("Badges", userBadges);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"User Info for **{ctx.TargetUser.Username}#{ctx.TargetUser.Discriminator}**")
                .AddEmbed(basicUserInfoEmbed).AsEphemeral());
            return;
        }

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
            roles = "";
            foreach (var role in member.Roles.OrderBy(role => role.Position).Reverse()) roles += role.Mention + " ";
        }

        var embed = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor($"{member.Color}"))
            .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
            .AddField("User Mention", member.Mention)
            .AddField("User ID", $"{member.Id}")
            .AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)")
            .AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)")
            .AddField("Roles", roles)
            .WithThumbnail(member.AvatarUrl)
            .WithTimestamp(DateTime.UtcNow);

        if (acknowledgements != null) embed.AddField("Acknowledgements", acknowledgements);

        if (member.PremiumSince != null)
        {
            var PremiumSinceUtc = member.PremiumSince.Value.UtcDateTime;
            var unixTime = ((DateTimeOffset)PremiumSinceUtc).ToUnixTimeSeconds();
            var boostingSince = $"Boosting since <t:{unixTime}:R> (<t:{unixTime}:F>";

            embed.AddField("Server Booster", boostingSince);
        }

        var badges = UserBadgeHelper.GetBadges(ctx.TargetUser);
        if (badges != "") embed.AddField("Badges", badges);

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{member.Username}#{member.Discriminator}**").AddEmbed(embed)
            .AsEphemeral());
    }
}
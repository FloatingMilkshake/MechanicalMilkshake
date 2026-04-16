namespace MechanicalMilkshake.Commands;

[RequirePermissions(DiscordPermission.BanMembers)]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal class BanCommands
{
    [Command("ban")]
    [Description("Ban a user. They will not be able to rejoin unless unbanned.")]
    public static async Task BanCommandAsync(SlashCommandContext ctx,
        [Parameter("user"), Description("The user to ban.")] DiscordUser userToBan,
        [Parameter("reason"), Description("The reason for the ban.")] [MinMaxLength(maxLength: 1500)]
        string reason = "No reason provided.")
    {
        await ctx.DeferResponseAsync(true);

        DiscordMember targetMember = default;
        try
        {
            targetMember = await ctx.Guild.GetMemberAsync(userToBan.Id);
        }
        catch (NotFoundException)
        {
            // not in server
        }

        if (!ctx.Member.CanModerate(targetMember))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have permission to ban **{userToBan.GetFullUsername()}**!")
                .AsEphemeral(true));
            return;
        }

        if (!ctx.Guild.CurrentMember.CanModerate(targetMember))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"I don't have permission to ban **{userToBan.GetFullUsername()}**! Please check the role hierarchy and permissions.")
                .AsEphemeral(true));
            return;
        }

        bool isUserAlreadyBanned = false;
        try
        {
            var ban = await ctx.Guild.GetBanAsync(userToBan);
            if (ban != default)
                isUserAlreadyBanned = true;
        }
        catch (NotFoundException)
        {
            // User isn't banned, handled below
        }

        if (isUserAlreadyBanned)
        {
            // Unban first to update the reason
            // I hate doing this but Discord doesn't update the reason if we just try to ban again without unbanning first
            await ctx.Guild.UnbanMemberAsync(userToBan, "Unbanning to ban with new reason");
        }

        await ctx.Guild.BanMemberAsync(userToBan.Id, TimeSpan.Zero, $"Banned by {ctx.User.Username}: {reason}");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("User banned successfully.").AsEphemeral(true));

        await ctx.Channel.SendMessageAsync($"{userToBan.Mention} has been banned: **{reason}**");
    }

    [Command("unban")]
    [Description("Unban a user.")]
    public static async Task UnbanCommandAsync(SlashCommandContext ctx,
        [Parameter("user"), Description("The user to unban.")] DiscordUser userToUnban)
    {
        await ctx.DeferResponseAsync(true);

        try
        {
            await ctx.Guild.UnbanMemberAsync(userToUnban, $"Unbanned by {ctx.User.Username}");
        }
        catch (NotFoundException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"**{userToUnban.GetFullUsername()}** isn't banned!")
                .AsEphemeral(true));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User unbanned successfully.").AsEphemeral(true));
        await ctx.Channel.SendMessageAsync(
            $"Successfully unbanned **{userToUnban.GetFullUsername()}**!");
    }
}

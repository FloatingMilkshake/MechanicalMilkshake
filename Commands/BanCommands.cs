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

        if (targetMember != default && ctx.Member.Hierarchy <= targetMember.Hierarchy)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have permission to ban **{userToBan.GetFullUsername()}**!")
                .AsEphemeral(true));
            return;
        }

        try
        {
            await ctx.Guild.BanMemberAsync(userToBan.Id, TimeSpan.Zero, reason);
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"I don't have permission to ban **{userToBan.GetFullUsername()}**! Please check the role hierarchy and permissions.")
                .AsEphemeral(true));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User banned successfully.").AsEphemeral(true));
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
            await ctx.Guild.UnbanMemberAsync(userToUnban);
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"I don't have permission to unban **{userToUnban.GetFullUsername()}**! Please check the role hierarchy and permissions.")
                .AsEphemeral(true));
            return;
        }
        catch (Exception e)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Hmm, something went wrong while trying to unban that user! Discord says the error was `{e.Message}`.")
                .AsEphemeral(true));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User unbanned successfully.").AsEphemeral(true));
        await ctx.Channel.SendMessageAsync(
            $"Successfully unbanned **{userToUnban.GetFullUsername()}**!");
    }
}

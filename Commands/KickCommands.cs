namespace MechanicalMilkshake.Commands;

internal class KickCommands
{
    [Command("kick")]
    [Description("Kick a user. They can rejoin the server with an invite.")]
    [RequirePermissions(DiscordPermission.KickMembers)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    public static async Task KickCommandAsync(SlashCommandContext ctx,
        [Parameter("user"), Description("The user to kick.")] DiscordUser userToKick,
        [Parameter("reason"), Description("The reason for the kick.")] [MinMaxLength(maxLength: 1500)]
        string reason = "No reason provided.")
    {
        await ctx.DeferResponseAsync(true);

        DiscordMember memberToKick;
        try
        {
            memberToKick = await ctx.Guild.GetMemberAsync(userToKick.Id);
        }
        catch (NotFoundException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Hmm, **{userToKick.GetFullUsername()}** doesn't seem to be in the server.")
                .AsEphemeral(true));
            return;
        }

        if (memberToKick != default && ctx.Member.Hierarchy <= memberToKick.Hierarchy)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have permission to ban **{memberToKick.GetFullUsername()}**!")
                .AsEphemeral(true));
            return;
        }

        try
        {
            await memberToKick.RemoveAsync(reason);
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"I don't have permission to kick **{userToKick.GetFullUsername()}**! Please check the role hierarchy and permissions.")
                .AsEphemeral(true));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User kicked successfully.").AsEphemeral(true));
        await ctx.Channel.SendMessageAsync($"{userToKick.Mention} has been kicked: **{reason}**");
    }
}

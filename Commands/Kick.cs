namespace MechanicalMilkshake.Commands;

[RequireGuild]
public class Kick
{
    [Command("kick")]
    [Description("Kick a user. They can rejoin the server with an invite.")]
    [RequirePermissions(DiscordPermissions.KickMembers)]
    public static async Task KickCommand(SlashCommandContext ctx,
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
        catch
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"Hmm, **{UserInfoHelpers.GetFullUsername(userToKick)}** doesn't seem to be in the server.")
                .AsEphemeral());
            return;
        }

        try
        {
            await memberToKick.RemoveAsync(reason);
        }
        catch
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to kick **{UserInfoHelpers.GetFullUsername(userToKick)}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User kicked successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"{userToKick.Mention} has been kicked: **{reason}**");
    }
}
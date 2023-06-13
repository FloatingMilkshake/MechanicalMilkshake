namespace MechanicalMilkshake.Commands;

[SlashRequireGuild]
public class Kick : ApplicationCommandModule
{
    [SlashCommand("kick", "Kick a user. They can rejoin the server with an invite.", false)]
    [SlashCommandPermissions(Permissions.KickMembers)]
    public static async Task KickCommand(InteractionContext ctx,
        [Option("user", "The user to kick.")] DiscordUser userToKick,
        [Option("reason", "The reason for the kick.")] [MaximumLength(1500)]
        string reason = "No reason provided.")
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral());

        DiscordMember memberToKick;
        try
        {
            memberToKick = await ctx.Guild.GetMemberAsync(userToKick.Id);
        }
        catch
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
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
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to kick **{UserInfoHelpers.GetFullUsername(userToKick)}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User kicked successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"{userToKick.Mention} has been kicked: **{reason}**");
    }
}
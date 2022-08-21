namespace MechanicalMilkshake.Commands;

[SlashRequireGuild]
public class Kick : ApplicationCommandModule
{
    [SlashCommand("kick", "Kick a user. They can rejoin the server with an invite.", false)]
    [SlashCommandPermissions(Permissions.KickMembers)]
    public async Task KickCommand(InteractionContext ctx, [Option("user", "The user to kick.")] DiscordUser userToKick,
        [Option("reason", "The reason for the kick.")]
        string reason = "No reason provided.")
    {
        DiscordMember memberToKick;
        try
        {
            memberToKick = await ctx.Guild.GetMemberAsync(userToKick.Id);
        }
        catch
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Hmm, **{userToKick.Username}#{userToKick.Discriminator}** doesn't seem to be in the server.")
                .AsEphemeral());
            return;
        }

        try
        {
            await memberToKick.RemoveAsync(reason);
        }
        catch
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to kick **{userToKick.Username}#{userToKick.Discriminator}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent("User kicked successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"{userToKick.Mention} has been kicked: **{reason}**");
    }
}
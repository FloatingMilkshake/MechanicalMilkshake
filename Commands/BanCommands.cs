namespace MechanicalMilkshake.Commands;

[RequireGuild]
public class BanCommands
{
    [Command("ban")]
    [Description("Ban a user. They will not be able to rejoin unless unbanned.")]
    [RequirePermissions(DiscordPermission.BanMembers)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild)]
    public static async Task BanCommand(SlashCommandContext ctx,
        [Parameter("user"), Description("The user to ban.")] DiscordUser userToBan,
        [Parameter("reason"), Description("The reason for the ban.")] [MinMaxLength(maxLength: 1500)]
        string reason = "No reason provided.")
    {
        await ctx.DeferResponseAsync(true);

        try
        {
            await ctx.Guild.BanMemberAsync(userToBan.Id, TimeSpan.Zero, reason);
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to ban **{UserInfoHelpers.GetFullUsername(userToBan)}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }
        catch (Exception e)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Hmm, something went wrong while trying to ban that user!\n\nThis was Discord's response:\n> {e.Message}")
                .AsEphemeral());
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User banned successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"{userToBan.Mention} has been banned: **{reason}**");
    }

    [Command("unban")]
    [Description("Unban a user.")]
    [RequirePermissions(DiscordPermission.BanMembers)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild)]
    public static async Task UnbanCommand(SlashCommandContext ctx,
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
                    $"Something went wrong. You or I may not be allowed to unban **{UserInfoHelpers.GetFullUsername(userToUnban)}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }
        catch (Exception e)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Hmm, something went wrong while trying to unban that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                .AsEphemeral());
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User unbanned successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync(
            $"Successfully unbanned **{UserInfoHelpers.GetFullUsername(userToUnban)}**!");
    }
}
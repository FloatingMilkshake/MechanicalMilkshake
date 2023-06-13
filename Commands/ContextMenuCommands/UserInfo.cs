namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class UserInfo : ApplicationCommandModule
{
    [ContextMenu(ApplicationCommandType.UserContextMenu, "User Info")]
    public static async Task ContextUserInfo(ContextMenuContext ctx)
    {
        DiscordEmbed userInfoEmbed;

        try
        {
            // Try to get member and get user info embed with extended information
            var member = await ctx.Guild.GetMemberAsync(ctx.TargetUser.Id);
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(member);
        }
        catch (NotFoundException)
        {
            // Member cannot be fetched (so is probably not in the guild); get user info embed with basic information
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(ctx.TargetUser);
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{UserInfoHelpers.GetFullUsername(ctx.TargetUser)}**")
            .AddEmbed(userInfoEmbed).AsEphemeral());
    }
}
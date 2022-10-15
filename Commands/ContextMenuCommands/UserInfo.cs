namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class UserInfo : ApplicationCommandModule
{
    [ContextMenu(ApplicationCommandType.UserContextMenu, "User Info")]
    public async Task ContextUserInfo(ContextMenuContext ctx)
    {
        DiscordMember member;
        DiscordEmbed userInfoEmbed;

        try
        {
            member = await ctx.Guild.GetMemberAsync(ctx.TargetUser.Id);
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(member);
        }
        catch (NotFoundException)
        {
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(ctx.TargetUser);
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{ctx.TargetUser.Username}#{ctx.TargetUser.Discriminator}**")
            .AddEmbed(userInfoEmbed).AsEphemeral());
    }
}
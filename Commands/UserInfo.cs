namespace MechanicalMilkshake.Commands;

public class UserInfo : ApplicationCommandModule
{
    [SlashCommand("userinfo", "Returns information about the provided server member.")]
    [SlashRequireGuild]
    public static async Task UserInfoCommand(InteractionContext ctx,
        [Option("user", "The user to look up information for. Defaults to yourself.")]
        DiscordUser user = null)
    {
        DiscordMember member;
        DiscordEmbed userInfoEmbed;

        user ??= ctx.User;

        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(member);
        }
        catch (NotFoundException)
        {
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(user);
        }
        
        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{user.Username}#{user.Discriminator}**").AddEmbed(userInfoEmbed));
    }
}
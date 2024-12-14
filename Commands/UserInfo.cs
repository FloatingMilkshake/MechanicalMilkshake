namespace MechanicalMilkshake.Commands;

public class UserInfo
{
    [Command("userinfo")]
    [Description("Returns information about the provided user.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task UserInfoCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("user"), Description("The user to look up information for. Defaults to yourself.")]
        DiscordUser user = null)
    {
        DiscordEmbed userInfoEmbed;

        user ??= ctx.User;

        try
        {
            if (ctx.Guild is not null)
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(member);
            }
            else
            {
                userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(user);
            }
        }
        catch (NotFoundException)
        {
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(user);
        }

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{UserInfoHelpers.GetFullUsername(user)}**").AddEmbed(userInfoEmbed));
    }
}
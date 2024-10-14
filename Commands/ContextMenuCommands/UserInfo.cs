namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class UserInfo
{
    [Command("User Info")]
    [AllowedProcessors(typeof(UserCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    public static async Task ContextUserInfo(CommandContext ctx, DiscordUser targetUser)
    {
        DiscordEmbed userInfoEmbed;

        try
        {
            // Try to get member and get user info embed with extended information
            if (ctx.Guild is not null)
            {
                var member = await ctx.Guild.GetMemberAsync(targetUser.Id);
                userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(member);   
            }
            else
            {
                userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(targetUser);
            }
        }
        catch (NotFoundException)
        {
            // Member cannot be fetched (so is probably not in the guild); get user info embed with basic information
            userInfoEmbed = await UserInfoHelpers.GenerateUserInfoEmbed(targetUser);
        }

        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"User Info for **{UserInfoHelpers.GetFullUsername(targetUser)}**")
            .AddEmbed(userInfoEmbed).AsEphemeral());
    }
}
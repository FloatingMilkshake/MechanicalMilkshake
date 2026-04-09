namespace MechanicalMilkshake.Commands;

[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
internal class UserInfoCommands
{
    [Command("User Info")]
    [AllowedProcessors(typeof(UserCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    public static async Task UserInfoUserContextMenuCommandAsync(CommandContext ctx, DiscordUser targetUser)
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
            .AddEmbed(userInfoEmbed).AsEphemeral(true));
    }

    [Command("userinfo")]
    [Description("Returns information about the provided user.")]
    public static async Task UserInfoCommandAsync(SlashCommandContext ctx,
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
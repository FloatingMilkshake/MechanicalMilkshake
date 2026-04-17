namespace MechanicalMilkshake.Commands;

[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
[InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
internal class UserInfoCommands
{
    [Command("User Info")]
    [AllowedProcessors(typeof(UserCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    public static async Task UserInfoUserContextMenuCommandAsync(UserCommandContext ctx, DiscordUser targetUser)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(false));

        DiscordEmbed userInfoEmbed;

        try
        {
            // Try to get member and get user info embed with extended information
            if (ctx.Guild is not null)
            {
                var member = await ctx.Guild.GetMemberAsync(targetUser.Id);
                userInfoEmbed = member.CreateUserInfoEmbed();
            }
            else
            {
                userInfoEmbed = targetUser.CreateUserInfoEmbed();
            }
        }
        catch (NotFoundException)
        {
            // Member cannot be fetched (so is probably not in the guild); get user info embed with basic information
            userInfoEmbed = targetUser.CreateUserInfoEmbed();
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"User Info for **{targetUser.GetFullUsername()}**")
            .AddEmbed(userInfoEmbed).AsEphemeral(true)
            .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
    }

    [Command("userinfo")]
    [Description("Returns information about the provided user.")]
    public static async Task UserInfoCommandAsync(SlashCommandContext ctx,
        [Parameter("user"), Description("The user to look up information for. Defaults to yourself.")]
        DiscordUser user = null)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(false));

        DiscordEmbed userInfoEmbed;

        user ??= ctx.User;

        try
        {
            if (ctx.Guild is not null)
            {
                var member = await ctx.Guild.GetMemberAsync(user.Id);
                userInfoEmbed = member.CreateUserInfoEmbed();
            }
            else
            {
                userInfoEmbed = user.CreateUserInfoEmbed();
            }
        }
        catch (NotFoundException)
        {
            userInfoEmbed = user.CreateUserInfoEmbed();
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"User Info for **{user.GetFullUsername()}**")
            .AddEmbed(userInfoEmbed)
            .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
    }
}

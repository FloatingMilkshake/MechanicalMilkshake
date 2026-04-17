namespace MechanicalMilkshake.Commands;

internal class AvatarCommands
{
    [Command("Avatar")]
    [AllowedProcessors(typeof(UserCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task AvatarUserContextMenuCommandAsync(UserCommandContext ctx, DiscordUser targetUser)
    {
        // Buttons shown to user to select which avatar to get if the target user has a server avatar set
        DiscordButtonComponent serverAvatarButton =
            new(DiscordButtonStyle.Primary, "button-callback-avatar-user-context-command-server-avatar", "Server Avatar");
        DiscordButtonComponent userAvatarButton =
            new(DiscordButtonStyle.Primary, "button-callback-avatar-user-context-command-user-avatar", "User Avatar");

        DiscordMember member = default;
        try
        {
            member = await ctx.Guild.GetMemberAsync(targetUser.Id);
        }
        catch (NotFoundException)
        {
            // User is not in the server, so no guild avatar available
        }

        if (member == default || member.GuildAvatarUrl is null)
            // User is not in the server or has no guild avatar; show global avatar
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"{targetUser.AvatarUrl}".Replace("size=1024", "size=4096"))
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
        else
            // User is in the server and has a guild avatar; show buttons to select which avatar to get
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"You requested the avatar for {targetUser.Mention}. Please choose one of the options below.")
                .AddActionRowComponent(serverAvatarButton, userAvatarButton)
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
    }

    [Command("avatar")]
    [Description("Returns the avatar of the provided user. Defaults to yourself if no user is provided.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task AvatarCommandAsync(SlashCommandContext ctx,
        [Parameter("user"), Description("The user whose avatar to get.")]
        DiscordUser user = null)
    {
        DiscordButtonComponent serverAvatarButton =
            new(DiscordButtonStyle.Primary, "button-callback-avatar-user-context-command-server-avatar", "Server Avatar");
        DiscordButtonComponent userAvatarButton =
            new(DiscordButtonStyle.Primary, "button-callback-avatar-user-context-command-user-avatar", "User Avatar");

        user ??= ctx.User;

        DiscordMember member = default;
        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch (NotFoundException)
        {
            // User is not in the server, so no guild avatar available
        }

        if (member == default || member.GuildAvatarUrl is null)
            await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent(
                    $"{user.AvatarUrl}".Replace("size=1024", "size=4096"))
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
        else
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"You requested the avatar for {user.Mention}. Please choose one of the options below.")
                .AddActionRowComponent(serverAvatarButton, userAvatarButton)
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }
}

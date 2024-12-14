namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class Avatar
{
    [Command("Avatar")]
    [AllowedProcessors(typeof(UserCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.UserContextMenu)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task ContextAvatar(CommandContext ctx, DiscordUser targetUser)
    {
        // Buttons shown to user to select which avatar to get if the target user has a server avatar set
        DiscordButtonComponent serverAvatarButton =
            new(DiscordButtonStyle.Primary, "server-avatar-ctx-cmd-button", "Server Avatar");
        DiscordButtonComponent userAvatarButton =
            new(DiscordButtonStyle.Primary, "user-avatar-ctx-cmd-button", "User Avatar");

        DiscordMember member = default;
        try
        {
            member = await ctx.Guild.GetMemberAsync(targetUser.Id);
        }
        catch
        {
            // User is not in the server, so no guild avatar available
        }

        if (member == default || member.GuildAvatarUrl is null)
            // User is not in the server or has no guild avatar; show global avatar
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"{targetUser.AvatarUrl}".Replace("size=1024", "size=4096")).AsEphemeral());
        else
            // User is in the server and has a guild avatar; show buttons to select which avatar to get
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"You requested the avatar for {targetUser.Mention}. Please choose one of the options below.")
                .AddComponents(serverAvatarButton, userAvatarButton).AsEphemeral());
    }
}
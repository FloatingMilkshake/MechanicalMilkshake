namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class Avatar : ApplicationCommandModule
{
    [ContextMenu(ApplicationCommandType.UserContextMenu, "Avatar")]
    public static async Task ContextAvatar(ContextMenuContext ctx)
    {
        // Buttons shown to user to select which avatar to get if the target user has a server avatar set
        DiscordButtonComponent serverAvatarButton =
            new(ButtonStyle.Primary, "server-avatar-ctx-cmd-button", "Server Avatar");
        DiscordButtonComponent userAvatarButton =
            new(ButtonStyle.Primary, "user-avatar-ctx-cmd-button", "User Avatar");

        DiscordMember member = default;
        try
        {
            member = await ctx.Guild.GetMemberAsync(ctx.TargetUser.Id);
        }
        catch
        {
            // User is not in the server, so no guild avatar available
        }

        if (member == default || member.GuildAvatarUrl is null)
            // User is not in the server or has no guild avatar; show global avatar
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"{ctx.TargetUser.AvatarUrl}".Replace("size=1024", "size=4096")).AsEphemeral());
        else
            // User is in the server and has a guild avatar; show buttons to select which avatar to get
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"You requested the avatar for {ctx.TargetUser.Mention}. Please choose one of the options below.")
                .AddComponents(serverAvatarButton, userAvatarButton).AsEphemeral());
    }
}
namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class Avatar : ApplicationCommandModule
{
    [ContextMenu(ApplicationCommandType.UserContextMenu, "Avatar")]
    public async Task ContextAvatar(ContextMenuContext ctx)
    {
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

        if (member == default || member.GuildAvatarUrl == null)
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent($"{ctx.TargetUser.AvatarUrl}".Replace("size=1024", "size=4096")).AsEphemeral());
        else
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"You requested the avatar for {ctx.TargetUser.Mention}. Please choose one of the options below.")
                .AddComponents(serverAvatarButton, userAvatarButton).AsEphemeral());
    }
}
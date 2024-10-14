namespace MechanicalMilkshake.Commands;

public class Avatar
{
    [Command("avatar")]
    [Description("Returns the avatar of the provided user. Defaults to yourself if no user is provided.")]
    public static async Task AvatarCommand(SlashCommandContext ctx,
        [Parameter("user"), Description("The user whose avatar to get.")]
        DiscordUser user = null)
    {
        DiscordButtonComponent serverAvatarButton =
            new(DiscordButtonStyle.Primary, "server-avatar-ctx-cmd-button", "Server Avatar");
        DiscordButtonComponent userAvatarButton =
            new(DiscordButtonStyle.Primary, "user-avatar-ctx-cmd-button", "User Avatar");

        user ??= ctx.User;

        DiscordMember member = default;
        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch
        {
            // User is not in the server, so no guild avatar available
        }

        if (member == default || member.GuildAvatarUrl is null)
            await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent(
                    $"{user.AvatarUrl}".Replace("size=1024", "size=4096")));
        else
            await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"You requested the avatar for {user.Mention}. Please choose one of the options below.")
                .AddComponents(serverAvatarButton, userAvatarButton));
    }
}
namespace MechanicalMilkshake.Commands;

[RequireGuild]
public class Nickname
{
    [Command("nickname")]
    [Description("Changes my nickname.")]
    [RequirePermissions(DiscordPermission.ChangeNickname, DiscordPermission.ManageNicknames)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild)]
    public static async Task NicknameCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("nickname"), Description("What to change my nickname to. Leave this blank to clear it.")] [MinMaxLength(maxLength: 32)]
        string nickname = null)
    {
        var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
        await bot.ModifyAsync(x =>
        {
            x.Nickname = nickname;
            x.AuditLogReason = $"Nickname changed by {ctx.User.Username} ({ctx.User.Id}).";
        });

        if (nickname is not null)
            await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent(
                    $"Nickname changed to **{nickname}** successfully!"));
        else
            await ctx.RespondAsync(
                new DiscordInteractionResponseBuilder().WithContent("Nickname cleared successfully!"));
    }
}
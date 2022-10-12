namespace MechanicalMilkshake.Commands;

[SlashRequireGuild]
public class Nickname : ApplicationCommandModule
{
    [SlashCommand("nickname", "Changes my nickname.", false)]
    [SlashCommandPermissions(Permissions.ManageNicknames)]
    public async Task NicknameCommand(InteractionContext ctx,
        [Option("nickname", "What to change my nickname to. Leave this blank to clear it.")] [MaximumLength(32)]
        string nickname = null)
    {
        var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
        await bot.ModifyAsync(x =>
        {
            x.Nickname = nickname;
            x.AuditLogReason = $"Nickname changed by {ctx.User.Username} (ID: {ctx.User.Id}).";
        });

        if (nickname != null)
            await ctx.CreateResponseAsync(
                new DiscordInteractionResponseBuilder().WithContent(
                    $"Nickname changed to **{nickname}** successfully!"));
        else
            await ctx.CreateResponseAsync(
                new DiscordInteractionResponseBuilder().WithContent("Nickname cleared successfully!"));
    }
}
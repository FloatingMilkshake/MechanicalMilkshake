namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class Lookup : ApplicationCommandModule
{
    [ContextMenu(ApplicationCommandType.UserContextMenu, "Lookup")]
    public async Task ContextLookup(ContextMenuContext ctx)
    {
        var msSinceEpoch = ctx.TargetUser.Id >> 22;
        var msUnix = msSinceEpoch + 1420070400000;
        var createdAt = $"{msUnix / 1000}";

        var embed = new DiscordEmbedBuilder()
            .WithThumbnail($"{ctx.TargetUser.AvatarUrl}")
            .AddField("ID", $"{ctx.TargetUser.Id}")
            .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

        var badges = UserBadgeHelper.GetBadges(ctx.TargetUser);
        if (badges != "") embed.AddField("Badges", badges);

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"Information about **{ctx.TargetUser.Username}#{ctx.TargetUser.Discriminator}**:")
            .AddEmbed(embed).AsEphemeral());
    }
}
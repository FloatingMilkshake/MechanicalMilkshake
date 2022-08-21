namespace MechanicalMilkshake.Commands;

public class Lookup : ApplicationCommandModule
{
    [SlashCommand("lookup", "Look up a user not in the current server.")]
    public async Task LookupCommand(InteractionContext ctx,
        [Option("user", "The user you want to look up.")]
        DiscordUser user)
    {
        var msSinceEpoch = user.Id >> 22;
        var msUnix = msSinceEpoch + 1420070400000;
        var createdAt = $"{msUnix / 1000}";

        var embed = new DiscordEmbedBuilder()
            .WithThumbnail($"{user.AvatarUrl}")
            .AddField("ID", $"{user.Id}")
            .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

        var badges = UserBadgeHelper.GetBadges(user);
        if (badges != "") embed.AddField("Badges", badges);

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"Information about **{user.Username}#{user.Discriminator}**:").AddEmbed(embed));
    }
}
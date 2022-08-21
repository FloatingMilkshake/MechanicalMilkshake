namespace MechanicalMilkshake.Commands;

public class ServerInfo : ApplicationCommandModule
{
    [SlashCommand("serverinfo", "Returns information about the server.")]
    [SlashRequireGuild]
    public async Task ServerInfoCommand(InteractionContext ctx)
    {
        var description = "None";

        if (ctx.Guild.Description is not null) description = ctx.Guild.Description;

        var msSinceEpoch = ctx.Guild.Id >> 22;
        var msUnix = msSinceEpoch + 1420070400000;
        var createdAt = $"{msUnix / 1000}";

        var botUserAsMember = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);

        var embed = new DiscordEmbedBuilder()
            .WithColor(new DiscordColor($"{botUserAsMember.Color}"))
            .AddField("Server Owner", $"{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}", true)
            .AddField("Channels", $"{ctx.Guild.Channels.Count}", true)
            .AddField("Members", $"{ctx.Guild.MemberCount}", true)
            .AddField("Roles", $"{ctx.Guild.Roles.Count}", true)
            .WithThumbnail($"{ctx.Guild.IconUrl}")
            .AddField("Description", $"{description}", true)
            .WithFooter($"Server ID: {ctx.Guild.Id}")
            .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)", true);

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"Server Info for **{ctx.Guild.Name}**").AddEmbed(embed));
    }
}
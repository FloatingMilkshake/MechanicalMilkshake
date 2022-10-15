namespace MechanicalMilkshake.Commands;

public class ServerInfo : ApplicationCommandModule
{
    [SlashCommand("serverinfo", "Returns information about the server.")]
    [SlashRequireGuild]
    public static async Task ServerInfoCommand(InteractionContext ctx)
    {
        var description = "None";

        if (ctx.Guild.Description is not null) description = ctx.Guild.Description;

        var msSinceEpoch = ctx.Guild.Id >> 22;
        var msUnix = msSinceEpoch + 1420070400000;
        var createdAt = $"{msUnix / 1000}";

        var botUserAsMember = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);

        var embed = new DiscordEmbedBuilder()
            .WithColor(Program.botColor)
            .AddField("Server Owner", $"{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}")
            .AddField("Description", $"{description}")
            .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)")
            .AddField("Channels", $"{ctx.Guild.Channels.Count}", true)
            .AddField("Members", $"{ctx.Guild.MemberCount}", true)
            .AddField("Roles", $"{ctx.Guild.Roles.Count}", true)
            .WithThumbnail($"{ctx.Guild.IconUrl}")
            .WithFooter($"Server ID: {ctx.Guild.Id}");

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent($"Server Info for **{ctx.Guild.Name}**").AddEmbed(embed));
    }
}
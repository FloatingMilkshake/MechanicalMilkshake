namespace MechanicalMilkshake.Commands;

public class ServerInfo : ApplicationCommandModule
{
    [SlashCommand("serverinfo", "Returns information about the server.")]
    [SlashRequireGuild]
    public static async Task ServerInfoCommand(InteractionContext ctx)
    {
        var description = "None";

        if (ctx.Guild.Description is not null) description = ctx.Guild.Description;

        var createdAt = $"{IdHelpers.GetCreationTimestamp(ctx.Guild.Id, true)}";

        var categoryCount = ctx.Guild.Channels.Count(channel => channel.Value.Type == ChannelType.Category);

        var embed = new DiscordEmbedBuilder()
            .WithColor(Program.BotColor)
            .AddField("Server Owner", $"{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}")
            .AddField("Description", $"{description}")
            .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)")
            .AddField("Channels", $"{ctx.Guild.Channels.Count - categoryCount}", true)
            .AddField("Categories", $"{categoryCount}", true)
            .AddField("Roles", $"{ctx.Guild.Roles.Count}", true)
            .AddField("Members (total)", $"{ctx.Guild.MemberCount}", true)
            .AddField("Bots", "loading... this might take a while", true)
            .AddField("Humans", "loading... this might take a while", true)
            .WithThumbnail($"{ctx.Guild.IconUrl}")
            .WithFooter($"Server ID: {ctx.Guild.Id}");

        var response = new DiscordInteractionResponseBuilder()
            .WithContent($"Server Info for **{ctx.Guild.Name}**").AddEmbed(embed);

        await ctx.CreateResponseAsync(response);

        var members = await ctx.Guild.GetAllMembersAsync();
        var botCount = members.Count(member => member.IsBot);
        var humanCount = ctx.Guild.MemberCount - botCount;

        var newEmbed = response.Embeds[0];

        newEmbed.Fields.FirstOrDefault(field => field.Name == "Bots")!.Value = $"{botCount}";
        newEmbed.Fields.FirstOrDefault(field => field.Name == "Humans")!.Value = $"{humanCount}";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response.Content).AddEmbed(embed));
    }
}
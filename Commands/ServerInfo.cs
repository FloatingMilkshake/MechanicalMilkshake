namespace MechanicalMilkshake.Commands;

public class ServerInfo
{
    [Command("serverinfo")]
    [Description("Look up information about a server.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)] // TODO: test in a guild w/ user-installed i.e. bot is not in guild
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild)]
    public static async Task ServerInfoCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("server"), Description("The ID of the server to look up. Defaults to the current server if you're not using this in DMs.")]
        string guildId = default)
    {
        await ctx.DeferResponseAsync();
        
        DiscordGuild guild;
        
        if (ctx.Channel.IsPrivate && guildId == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You can't use this command in DMs without specifying a server ID! Please try again."));
            return;
        }

        if (guildId == default)
        {
            guild = ctx.Guild;
        }
        else
        {
            try
            {
                guild = await ctx.Client.GetGuildAsync(Convert.ToUInt64(guildId));
            }
            catch (Exception ex) when (ex is UnauthorizedException or NotFoundException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "Sorry, I can't read the details for that server because I'm not in it or it doesn't exist!"));
                return;
            }
            catch (FormatException)
            {
                await ctx.FollowupAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "That doesn't look like a valid server ID! Please try again."));
                return;
            }
        }
        
        var description = "None";

        if (guild.Description is not null) description = guild.Description;

        var createdAt = $"{IdHelpers.GetCreationTimestamp(guild.Id, true)}";

        var categoryCount = guild.Channels.Count(channel => channel.Value.Type == DiscordChannelType.Category);

        var embed = new DiscordEmbedBuilder()
            .WithColor(Program.BotColor)
            .AddField("Server Owner", $"{UserInfoHelpers.GetFullUsername(await guild.GetGuildOwnerAsync())}")
            .AddField("Description", $"{description}")
            .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)")
            .AddField("Channels", $"{guild.Channels.Count - categoryCount}", true)
            .AddField("Categories", $"{categoryCount}", true)
            .AddField("Roles", $"{guild.Roles.Count}", true)
            .AddField("Members (total)", $"{guild.MemberCount}", true)
            .AddField("Bots", "loading... this might take a while", true)
            .AddField("Humans", "loading... this might take a while", true)
            .WithThumbnail($"{guild.IconUrl}")
            .WithFooter($"Server ID: {guild.Id}");

        var response = new DiscordFollowupMessageBuilder()
            .WithContent($"Server Info for **{guild.Name}**").AddEmbed(embed);

        await ctx.FollowupAsync(response);

        var members = await guild.GetAllMembersAsync().ToListAsync();
        var botCount = members.Count(member => member.IsBot);
        var humanCount = guild.MemberCount - botCount;

        var newEmbed = response.Embeds[0];

        newEmbed.Fields.FirstOrDefault(field => field.Name == "Bots")!.Value = $"{botCount}";
        newEmbed.Fields.FirstOrDefault(field => field.Name == "Humans")!.Value = $"{humanCount}";

        await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(response.Content).AddEmbed(embed));
    }
}
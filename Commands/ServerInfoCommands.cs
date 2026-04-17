namespace MechanicalMilkshake.Commands;

internal class ServerInfoCommands
{
    [Command("serverinfo")]
    [Description("Look up information about a server.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    public static async Task ServerInfoCommandAsync(SlashCommandContext ctx,
        [Parameter("server"), Description("The ID of the server to look up. Defaults to the current server if you're not using this in DMs.")]
        string guildId = default)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        DiscordGuild guild;

        if (ctx.Channel.IsPrivate && guildId == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You can't use this command in DMs without specifying a server ID! Please try again.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
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
                    .WithContent("Sorry, I can't read the details for that server because I'm not in it or it doesn't exist!")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }
            catch (FormatException)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("That doesn't look like a valid server ID! Please try again.")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
                return;
            }
        }

        var description = "None";

        if (guild.Description is not null) description = guild.Description;

        var createdAt = $"{guild.Id.ToUnixTimeSeconds()}";

        var categoryCount = guild.Channels.Count(channel => channel.Value.Type == DiscordChannelType.Category);

        var embed = new DiscordEmbedBuilder()
            .WithColor(Setup.Constants.BotColor)
            .AddField("Server Owner", $"{(await guild.GetGuildOwnerAsync()).GetFullUsername()}")
            .AddField("Description", $"{description}")
            .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)")
            .AddField("Channels", $"{guild.Channels.Count - categoryCount}", true)
            .AddField("Categories", $"{categoryCount}", true)
            .AddField("Roles", $"{guild.Roles.Count}", true)
            .AddField("Members", $"{guild.MemberCount}", true)
            .WithThumbnail($"{guild.IconUrl}")
            .WithFooter($"Server ID: {guild.Id}");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Server Info for **{guild.Name}**")
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }
}

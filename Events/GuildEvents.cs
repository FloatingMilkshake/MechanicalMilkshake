namespace MechanicalMilkshake.Events;

public class GuildEvents
{
    private static readonly List<ulong> UnavailableGuilds = new();
    
    public static async Task GuildCreated(DiscordClient client, GuildCreateEventArgs e)
    {
        // TODO: dont hardcode ID, probably make it a config value
        var chan = await client.GetChannelAsync(1019068256163729461);
        
        if (UnavailableGuilds.Contains(e.Guild.Id))
        {
            var embed = new DiscordEmbedBuilder().WithColor(Program.BotColor).WithTitle("Guild no longer unavailable")
                .WithDescription(
                    $"The guild {e.Guild.Name} (`{e.Guild.Id}`) was previously unavailable, but is now available again.")
                .AddField("Members", e.Guild.MemberCount.ToString(), true).AddField("Owner",
                    $"{e.Guild.Owner.Username}#{e.Guild.Owner.Discriminator} (`{e.Guild.Owner.Id}`)", true);

            await chan.SendMessageAsync(embed);
            return;
        }
        
        await SendGuildEventLogEmbed(chan, e.Guild, true);
    }

    public static async Task GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
    {
        if (e.Guild.IsUnavailable)
        {
            UnavailableGuilds.Add(e.Guild.Id);
            return;
        }
        
        var chan = await client.GetChannelAsync(1019068256163729461);
        await SendGuildEventLogEmbed(chan, e.Guild, false);
    }

    private static async Task SendGuildEventLogEmbed(DiscordChannel chan, DiscordGuild guild, bool isJoin)
    {
        DiscordEmbedBuilder embed = new()
        {
            Title = isJoin ? "I've been added to a server!" : "I've been removed from a server!",
            Color = Program.BotColor
        };

        embed.WithThumbnail(guild.IconUrl);

        embed.AddField("Server", $"{guild.Name}\n(`{guild.Id}`)", true);
        embed.AddField("Members", guild.MemberCount.ToString(), true);

        DiscordEmbedBuilder userInfoEmbed;
        try
        {
            userInfoEmbed =
                new DiscordEmbedBuilder(await UserInfoHelpers.GenerateUserInfoEmbed((DiscordUser)guild.Owner));
            userInfoEmbed.WithColor(Program.BotColor);
            userInfoEmbed.WithTitle("User Info for Server Owner");
            userInfoEmbed.WithDescription($"{guild.Owner.Username}#{guild.Owner.Discriminator}");
        }
        catch (Exception ex)
        {
            userInfoEmbed = new DiscordEmbedBuilder().WithTitle("User Info for Server Owner")
                .WithDescription("Failed to fetch server owner.")
                .AddField("Exception", $"{ex.GetType()}: {ex.Message}");
        }

        await chan.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed).AddEmbed(userInfoEmbed));
    }
}
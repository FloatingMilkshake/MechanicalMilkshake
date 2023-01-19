namespace MechanicalMilkshake.Events;

public class GuildEvents
{
    public static async Task GuildCreated(DiscordClient client, GuildCreateEventArgs e)
    {
        // TODO: dont hardcode ID, probably make it a config value
        var chan = await client.GetChannelAsync(1019068256163729461);
        await SendGuildEventLogEmbed(chan, e.Guild, true);
    }

    public static async Task GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
    {
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

        DiscordEmbedBuilder userInfoEmbed = new DiscordEmbedBuilder(await UserInfoHelpers.GenerateUserInfoEmbed((DiscordUser)guild.Owner)); 
        userInfoEmbed.WithColor(Program.BotColor);
        userInfoEmbed.WithTitle("User Info for Server Owner");
        userInfoEmbed.WithDescription($"{guild.Owner.Username}#{guild.Owner.Discriminator}");

        await chan.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed).AddEmbed(userInfoEmbed));
    }
}
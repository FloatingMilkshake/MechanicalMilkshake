namespace MechanicalMilkshake.Events;

internal class GuildEvents
{
    internal static async Task HandleGuildCreatedEventAsync(DiscordClient _, GuildCreatedEventArgs e)
    {
        if (Setup.Configuration.Discord.Channels.GuildLogs is null)
            return;

        await SendGuildEventLogEmbed(Setup.Configuration.Discord.Channels.GuildLogs, e.Guild, Setup.Types.GuildEventType.Join);
    }

    internal static async Task HandleGuildDeletedEventAsync(DiscordClient _, GuildDeletedEventArgs e)
    {
        if (Setup.Configuration.Discord.Channels.GuildLogs is null)
            return;

        await SendGuildEventLogEmbed(Setup.Configuration.Discord.Channels.GuildLogs, e.Guild, Setup.Types.GuildEventType.Leave);
    }

    internal static async Task HandleGuildDownloadCompletedEventAsync(DiscordClient _, GuildDownloadCompletedEventArgs __)
    {
        Setup.State.Discord.GuildDownloadCompleted = true;
    }

    private static async Task SendGuildEventLogEmbed(DiscordChannel channel, DiscordGuild guild, Setup.Types.GuildEventType eventType)
    {
        DiscordEmbedBuilder embed = new()
        {
            Title = eventType == Setup.Types.GuildEventType.Join
                ? "I've been added to a server!"
                : "I've been removed from a server!",
            Color = Setup.Constants.BotColor
        };

        embed.WithThumbnail(guild.IconUrl);

        embed.AddField("Server", $"{guild.Name}\n(`{guild.Id}`)", true);
        embed.AddField("Members", guild.MemberCount.ToString(), true);

        DiscordEmbedBuilder userInfoEmbed;
        try
        {
            var owner = await guild.GetGuildOwnerAsync();
            userInfoEmbed = new DiscordEmbedBuilder((owner as DiscordUser).CreateUserInfoEmbed());
            userInfoEmbed.WithColor(Setup.Constants.BotColor);
            userInfoEmbed.WithTitle("User Info for Server Owner");
            userInfoEmbed.WithDescription($"{owner.GetFullUsername()}");
        }
        catch (Exception ex)
        {
            userInfoEmbed = new DiscordEmbedBuilder().WithTitle("User Info for Server Owner")
                .WithDescription("Failed to fetch server owner.")
                .AddField("Exception", $"```\n{ex.GetType()}: {ex.Message}\n```");
        }

        var msg = new DiscordMessageBuilder().AddEmbed(embed);

        // Only send owner info on join; will fail to fetch on leave
        if (eventType == Setup.Types.GuildEventType.Join)
            msg.AddEmbed(userInfoEmbed);

        await channel.SendMessageAsync(msg);
    }
}

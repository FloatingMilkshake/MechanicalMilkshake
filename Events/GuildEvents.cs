namespace MechanicalMilkshake.Events;

internal class GuildEvents
{
    internal static async Task HandleGuildCreatedEventAsync(DiscordClient _, GuildCreatedEventArgs e)
    {
        if (Setup.State.Discord.Channels.GuildLogs is not null)
            await SendGuildEventLogEmbed(Setup.State.Discord.Channels.GuildLogs, e.Guild, GuildEventType.Join);

        await CheckAndLeaveBlacklistedGuild(e.Guild);
    }

    internal static async Task HandleGuildDeletedEventAsync(DiscordClient _, GuildDeletedEventArgs e)
    {
        if (Setup.State.Discord.Channels.GuildLogs is not null)
            await SendGuildEventLogEmbed(Setup.State.Discord.Channels.GuildLogs, e.Guild, GuildEventType.Leave);
    }

    internal static async Task HandleGuildDownloadCompletedEventAsync(DiscordClient _, GuildDownloadCompletedEventArgs __)
    {
        Setup.State.Discord.GuildDownloadCompleted = true;
    }

    private static async Task CheckAndLeaveBlacklistedGuild(DiscordGuild guild)
    {
        var blacklist = await Setup.Storage.Redis.HashGetAllAsync("blacklistedGuilds");
        if (blacklist.Any(g => g.Name == guild.Id))
            await guild.LeaveAsync();
    }

    private static async Task SendGuildEventLogEmbed(DiscordChannel channel, DiscordGuild guild, GuildEventType eventType)
    {
        DiscordEmbedBuilder embed = new()
        {
            Title = eventType == GuildEventType.Join
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
        if (eventType == GuildEventType.Join)
            msg.AddEmbed(userInfoEmbed);

        await channel.SendMessageAsync(msg);
    }

    private enum GuildEventType
    {
        Join = 0,
        Leave = 1
    }
}

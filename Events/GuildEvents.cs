namespace MechanicalMilkshake.Events;

public class GuildEvents
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly List<ulong> UnavailableGuilds = [];
    private static DiscordChannel _guildLogChannel;
    
    public static async Task GuildCreated(DiscordClient client, GuildCreatedEventArgs e)
    {
        // Fail silently if log channel ID missing or invalid
        if (Program.ConfigJson.Logs.Guilds == "") return;
        if (!await GetFeedbackChannel()) return;
        
        if (UnavailableGuilds.Contains(e.Guild.Id))
        {
            var owner = await e.Guild.GetGuildOwnerAsync();
            var embed = new DiscordEmbedBuilder().WithColor(Program.BotColor).WithTitle("Guild no longer unavailable")
                .WithDescription(
                    $"The guild {e.Guild.Name} (`{e.Guild.Id}`) was previously unavailable, but is now available again.")
                .AddField("Members", e.Guild.MemberCount.ToString(), true).AddField("Owner",
                    $"{UserInfoHelpers.GetFullUsername(owner)} (`{owner.Id}`)", true);

            await _guildLogChannel.SendMessageAsync(embed);
            return;
        }
        
        await SendGuildEventLogEmbed(_guildLogChannel, e.Guild, true);
    }

    public static async Task GuildDeleted(DiscordClient client, GuildDeletedEventArgs e)
    {
        // Fail silently if log channel ID missing or invalid
        if (Program.ConfigJson.Logs.Guilds == "") return;
        if (!await GetFeedbackChannel()) return;
        
        if (e.Guild.IsUnavailable)
        {
            UnavailableGuilds.Add(e.Guild.Id);
            return;
        }
        
        await SendGuildEventLogEmbed(_guildLogChannel, e.Guild, false);
    }
    
    public static Task GuildDownloadCompleted(DiscordClient _, GuildDownloadCompletedEventArgs __)
    {
        Program.GuildDownloadCompleted = true;
        return Task.CompletedTask;
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
            var owner = await guild.GetGuildOwnerAsync();
            userInfoEmbed = new DiscordEmbedBuilder(await UserInfoHelpers.GenerateUserInfoEmbed(owner));
            userInfoEmbed.WithColor(Program.BotColor);
            userInfoEmbed.WithTitle("User Info for Server Owner");
            userInfoEmbed.WithDescription($"{UserInfoHelpers.GetFullUsername(owner)}");
        }
        catch (Exception ex)
        {
            userInfoEmbed = new DiscordEmbedBuilder().WithTitle("User Info for Server Owner")
                .WithDescription("Failed to fetch server owner.")
                .AddField("Exception", $"{ex.GetType()}: {ex.Message}");
        }

        var msg = new DiscordMessageBuilder().AddEmbed(embed);

        // Only send owner info on join; will fail to fetch on leave
        if (isJoin) msg.AddEmbed(userInfoEmbed);
        
        await chan.SendMessageAsync(msg);
    }

    private static async Task<bool> GetFeedbackChannel()
    {
        var success = false;

        try
        {
            var chanId = Convert.ToUInt64(Program.ConfigJson.Logs.Guilds);
            _guildLogChannel = await Program.Discord.GetChannelAsync(chanId);
            success = true;
        }
        catch (Exception ex) when (ex is FormatException or NotFoundException or UnauthorizedException)
        {
            // do nothing, will return false
        }

        return success;
    }
}

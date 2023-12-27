namespace MechanicalMilkshake.Events;

public class GuildEvents
{
    // ReSharper disable once MemberCanBePrivate.Global
    public static readonly List<ulong> UnavailableGuilds = [];
    private static DiscordChannel _guildLogChannel;
    
    public static async Task GuildCreated(DiscordClient client, GuildCreateEventArgs e)
    {
        // Fail silently if log channel ID missing or invalid
        if (Program.ConfigJson.Logs.Guilds == "") return;
        if (!await GetFeedbackChannel()) return;
        
        if (UnavailableGuilds.Contains(e.Guild.Id))
        {
            var embed = new DiscordEmbedBuilder().WithColor(Program.BotColor).WithTitle("Guild no longer unavailable")
                .WithDescription(
                    $"The guild {e.Guild.Name} (`{e.Guild.Id}`) was previously unavailable, but is now available again.")
                .AddField("Members", e.Guild.MemberCount.ToString(), true).AddField("Owner",
                    $"{UserInfoHelpers.GetFullUsername(e.Guild.Owner)} (`{e.Guild.Owner.Id}`)", true);

            await _guildLogChannel.SendMessageAsync(embed);
            return;
        }
        
        await SendGuildEventLogEmbed(_guildLogChannel, e.Guild, true);
    }

    public static async Task GuildDeleted(DiscordClient client, GuildDeleteEventArgs e)
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
            userInfoEmbed.WithDescription($"{UserInfoHelpers.GetFullUsername(guild.Owner)}");
        }
        catch (Exception ex)
        {
            userInfoEmbed = new DiscordEmbedBuilder().WithTitle("User Info for Server Owner")
                .WithDescription("Failed to fetch server owner.")
                .AddField("Exception", $"{ex.GetType()}: {ex.Message}");
        }

        await chan.SendMessageAsync(new DiscordMessageBuilder().AddEmbed(embed).AddEmbed(userInfoEmbed));
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

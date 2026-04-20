namespace MechanicalMilkshake.Setup.Types;

internal sealed class TrackedKeyword
{
    [JsonProperty("keyword")] internal string Keyword { get; private set; }

    [JsonProperty("userId")] internal ulong UserId { get; private set; }

    [JsonProperty("matchWholeWord")] internal bool MatchWholeWord { get; private set; }

    [JsonProperty("ignoreBots")] internal bool IgnoreBots { get; private set; }

    [JsonProperty("assumePresence")] internal bool AssumePresence { get; private set; }

    [JsonProperty("userIgnoreList")] internal List<ulong> UserIgnoreList { get; private set; }

    [JsonProperty("channelIgnoreList")] internal List<ulong> ChannelIgnoreList { get; private set; }

    [JsonProperty("guildIgnoreList")] internal List<ulong> GuildIgnoreList { get; private set; }

    [JsonProperty("id")] internal ulong Id { get; private set; }

    [JsonProperty("guildId")] internal ulong GuildId { get; private set; }

    [JsonConstructor]
    internal TrackedKeyword(string keyword, ulong userId, bool matchWholeWord, bool ignoreBots, bool assumePresence,
        List<ulong> userIgnoreList, List<ulong> channelIgnoreList, List<ulong> guildIgnoreList, ulong id, ulong guildId)
    {
        Keyword = keyword;
        UserId = userId;
        MatchWholeWord = matchWholeWord;
        IgnoreBots = ignoreBots;
        AssumePresence = assumePresence;
        UserIgnoreList = userIgnoreList;
        ChannelIgnoreList = channelIgnoreList;
        GuildIgnoreList = guildIgnoreList;
        Id = id;
        GuildId = guildId;
    }

    internal async Task<DiscordEmbedBuilder> CreateDetailsEmbedAsync()
    {
        var ignoredUserMentions = "\n";
        foreach (var userToIgnore in UserIgnoreList)
        {
            ignoredUserMentions += $"- <@{userToIgnore}>\n";
        }

        if (ignoredUserMentions == "\n") ignoredUserMentions = " None\n";

        var ignoredChannelMentions = "\n";
        foreach (var channelToIgnore in ChannelIgnoreList)
        {
            ignoredChannelMentions += $"- <#{channelToIgnore}>\n";
        }

        if (ignoredChannelMentions == "\n") ignoredChannelMentions = " None\n";

        var ignoredGuildNames = "\n";
        foreach (var guildToIgnore in GuildIgnoreList)
        {
            var guild = Setup.State.Discord.Client.Guilds[guildToIgnore];
            ignoredGuildNames += $"- {guild.Name}\n";
        }

        if (ignoredGuildNames == "\n") ignoredGuildNames = " None\n";

        var matchWholeWord = MatchWholeWord.ToString().Trim();

        var limitedGuild = GuildId == default
            ? "None"
            : (await Setup.State.Discord.Client.GetGuildAsync(GuildId)).Name;

        DiscordEmbedBuilder embed = new()
        {
            Title = "Keyword Details",
            Color = Setup.Constants.BotColor,
            Description = Keyword
        };

        embed.AddField("Ignored Users", ignoredUserMentions, true);
        embed.AddField("Ignored Channels", ignoredChannelMentions, true);
        embed.AddField("Ignored Servers", ignoredGuildNames, true);
        embed.AddField("Ignore Bots", IgnoreBots.ToString(), true);
        embed.AddField("Match Whole Word", matchWholeWord, true);
        embed.AddField("Assume Presence", AssumePresence.ToString(), true);
        embed.AddField("Limited to Server", limitedGuild, true);

        return embed;
    }

    internal async Task SendAlertMessageAsync(DiscordMessage message, bool isEdit = false)
    {
        DiscordMember member;
        try
        {
            member = await message.Channel.Guild.GetMemberAsync(UserId);
        }
        catch (NotFoundException)
        {
            // User is not in guild. Skip.
            return;
        }

        DiscordEmbedBuilder embed = new()
        {
            Color = new DiscordColor("#7287fd"),
            Title = Keyword.Length > 225 ? "Tracked keyword triggered!" : $"Tracked keyword \"{Keyword}\" triggered!",
            Description = message.Content
        };

        if (isEdit)
            embed.WithFooter("This alert was triggered by an edit to the message.");

        embed.AddField("Author ID", $"{message.Author.Id}", true);
        embed.AddField("Author Mention", $"{message.Author.Mention}", true);

        embed.AddField("Context", $"{message.JumpLink}");

        try
        {
            await member.SendMessageAsync(embed);
        }
        catch (UnauthorizedException)
        {
            // User has DMs disabled. Oh well.
        }
    }
}

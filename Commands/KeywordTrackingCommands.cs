namespace MechanicalMilkshake.Commands;

[Command("track")]
[Description("Track or untrack keywords.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal class KeywordTrackingCommands
{
    [Command("add")]
    [Description("Track a new keyword.")]
    public static async Task TrackAddCommandAsync(SlashCommandContext ctx,
        [Parameter("keyword"), Description("The keyword or phrase to track.")]
        string keyword,
        [Parameter("match_whole_word"), Description("Whether you want to match the keyword only when it is a whole word. Defaults to False.")]
        bool matchWholeWord = false,
        [Parameter("ignore_bots"), Description("Whether to ignore messages from bots. Defaults to True.")]
        bool ignoreBots = true,
        [Parameter("assume_presence"), Description("Whether to assume you're present and ignore messages sent directly after your own. Defaults to True.")]
        bool assumePresence = true,
        [Parameter("user_ignore_list"), Description("Users to ignore. Use IDs and/or mentions. Separate with spaces.")]
        string userIgnoreList = null,
        [Parameter("channel_ignore_list"), Description("Channels to ignore. Use IDs only. Separate with spaces.")]
        string channelIgnoreList = null,
        [Parameter("server_ignore_list"), Description("Servers to ignore. Use IDs only. Separate with spaces.")]
        string guildIgnoreList = null,
        [Parameter("this_server_only"), Description("Whether to only notify you if the keyword is mentioned in this server. Defaults to True.")]
        bool currentGuildOnly = true)
    {
        await ctx.DeferResponseAsync(true);

        if (currentGuildOnly && guildIgnoreList is not null &&
            guildIgnoreList.Split(' ').Any(g => g == ctx.Guild.Id.ToString())) currentGuildOnly = false;

        var fields = await Setup.Storage.Redis.HashGetAllAsync("keywords");
        foreach (var field in fields)
        {
            var fieldValue = JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(field.Value);

            // If the keyword is already being tracked, delete the current entry
            // This way we don't end up with duplicate entries for keywords
            if (fieldValue.Keyword != keyword) continue;
            await Setup.Storage.Redis.HashDeleteAsync("keywords", fieldValue.Id);
            break;
        }

        List<string> checkedUsers = [];

        List<ulong> usersToIgnore = [];
        if (userIgnoreList is not null)
        {
            var users = userIgnoreList.Split(' ');
            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user)) continue;
                if (checkedUsers.Any(u => u.ToString() == user)) continue;
                checkedUsers.Add(user);

                var id = user.Contains('@') ? Setup.Constants.RegularExpressions.DiscordIdPattern.Match(user).ToString() : user;

                DiscordUser userToAdd;
                try
                {
                    userToAdd = await Setup.State.Discord.Client.GetUserAsync(Convert.ToUInt64(id));
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {user} as a user ID. Make sure it's formatted correctly! If you want to ignore multiple users, separate their mentions or IDs with a space.")
                        .AsEphemeral(true));
                    return;
                }

                usersToIgnore.Add(userToAdd.Id);
            }
        }

        List<string> checkedChannels = [];

        List<ulong> channelsToIgnore = [];
        if (channelIgnoreList is not null)
        {
            var channels = channelIgnoreList.Split(' ');
            foreach (var channel in channels)
            {
                if (string.IsNullOrWhiteSpace(channel)) continue;
                if (checkedChannels.Any(c => c.ToString() == channel)) continue;
                checkedChannels.Add(channel);

                var id = Setup.Constants.RegularExpressions.DiscordIdPattern.Match(channel).ToString();

                DiscordChannel channelToAdd;
                try
                {
                    channelToAdd = await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says that channel doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {channel} as a channel ID. Make sure it's formatted correctly! If you want to ignore multiple channels, please separate their IDs with a space.")
                        .AsEphemeral(true));
                    return;
                }

                channelsToIgnore.Add(channelToAdd.Id);
            }
        }

        List<string> checkedGuilds = [];

        List<ulong> guildsToIgnore = [];
        if (guildIgnoreList is not null)
        {
            var guilds = guildIgnoreList.Split(' ');
            foreach (var guild in guilds)
            {
                if (string.IsNullOrWhiteSpace(guild)) continue;
                if (checkedGuilds.Any(g => g.ToString() == guild)) continue;
                checkedGuilds.Add(guild);

                var id = Setup.Constants.RegularExpressions.DiscordIdPattern.Match(guild).ToString();

                DiscordGuild guildToAdd;
                try
                {
                    guildToAdd = await Setup.State.Discord.Client.GetGuildAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says that server doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {guild} as a server ID. Make sure it's formatted correctly! If you want to ignore multiple servers, please separate their IDs with a space.")
                        .AsEphemeral(true));
                    return;
                }

                guildsToIgnore.Add(guildToAdd.Id);
            }
        }

        Setup.Types.TrackedKeyword trackedKeyword = new()
        {
            Keyword = keyword,
            UserId = ctx.User.Id,
            MatchWholeWord = matchWholeWord,
            IgnoreBots = ignoreBots,
            AssumePresence = assumePresence,
            UserIgnoreList = usersToIgnore,
            ChannelIgnoreList = channelsToIgnore,
            GuildIgnoreList = guildsToIgnore,
            Id = ctx.Interaction.Id,
            GuildId = currentGuildOnly ? ctx.Guild.Id : default
        };

        await Setup.Storage.Redis.HashSetAsync("keywords", ctx.Interaction.Id,
            JsonConvert.SerializeObject(trackedKeyword));
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Done!").AsEphemeral(true));
    }

    [Command("edit")]
    [Description("Edit a tracked keyword.")]
    public static async Task TrackEditCommandAsync(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(Setup.Types.AutoCompleteProviders.TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to edit.")]
        string keyword,
        [Parameter("new_keyword"), Description("The new keyword or phrase to use instead.")]
        string newKeyword = null,
        [Parameter("match_whole_word"), Description("Whether you want to match the keyword only when it is a whole word.")]
        bool? matchWholeWord = null,
        [Parameter("ignore_bots"), Description("Whether to ignore messages from bots.")]
        bool? ignoreBots = null,
        [Parameter("assume_presence"), Description("Whether to assume you're present and ignore messages sent directly after your own.")]
        bool? assumePresence = null,
        [Parameter("user_ignore_list"), Description("Users to ignore. Use IDs and/or mentions. Separate with spaces.")]
        string userIgnoreList = null,
        [Parameter("channel_ignore_list"), Description("Channels to ignore. Use IDs only. Separate with spaces.")]
        string channelIgnoreList = null,
        [Parameter("server_ignore_list"), Description("Servers to ignore. Use IDs only. Separate with spaces.")]
        string guildIgnoreList = null,
        [Parameter("this_server_only"), Description("Whether to only notify you if the keyword is mentioned in this server.")]
        bool? currentGuildOnly = null)
    {
        await ctx.DeferResponseAsync(true);

        var allKeywordsRawData = await Setup.Storage.Redis.HashGetAllAsync("keywords");

        var userKeywords = allKeywordsRawData.Select(field => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(field.Value))
            .Where(x => x.UserId == ctx.User.Id).ToList();

        if (userKeywords.Count == 0)
        {
            var trackAddCommand = CommandHelpers.GetSlashCmdMention("track add");

            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have any tracked keywords! Add some with {trackAddCommand}."));

            return;
        }

        var thisKeywordRawData = allKeywordsRawData.FirstOrDefault(field =>
            JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(field.Value).Id.ToString() == keyword).Value;

        if (string.IsNullOrWhiteSpace(thisKeywordRawData))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that keyword! Please try again.").AsEphemeral(true));
            return;
        }

        if (newKeyword is null && matchWholeWord is null && ignoreBots is null && assumePresence is null &&
            userIgnoreList is null && channelIgnoreList is null && guildIgnoreList is null &&
            currentGuildOnly is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You didn't change anything! Nothing to do.").AsEphemeral(true));
            return;
        }

        var thisKeywordData = JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(thisKeywordRawData.ToString());

        if (newKeyword is not null)
            thisKeywordData.Keyword = newKeyword;
        if (matchWholeWord is not null)
            thisKeywordData.MatchWholeWord = matchWholeWord.Value;
        if (ignoreBots is not null)
            thisKeywordData.IgnoreBots = ignoreBots.Value;
        if (assumePresence is not null)
            thisKeywordData.AssumePresence = assumePresence.Value;

        List<string> checkedUsers = [];
        List<ulong> usersToIgnore = [];
        if (userIgnoreList is not null)
        {
            var users = userIgnoreList.Split(' ');
            foreach (var user in users)
            {
                if (string.IsNullOrWhiteSpace(user)) continue;
                if (checkedUsers.Any(u => u.ToString() == user)) continue;
                checkedUsers.Add(user);

                var id = user.Contains('@') ? Setup.Constants.RegularExpressions.DiscordIdPattern.Match(user).ToString() : user;

                DiscordUser userToAdd;
                try
                {
                    userToAdd = await Setup.State.Discord.Client.GetUserAsync(Convert.ToUInt64(id));
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {user} as a user ID. Make sure it's formatted correctly! If you want to ignore multiple users, separate their mentions or IDs with a space.")
                        .AsEphemeral(true));
                    return;
                }

                usersToIgnore.Add(userToAdd.Id);
            }
        }
        thisKeywordData.UserIgnoreList = usersToIgnore;

        List<string> checkedChannels = [];
        List<ulong> channelsToIgnore = [];
        if (channelIgnoreList is not null)
        {
            var channels = channelIgnoreList.Split(' ');
            foreach (var channel in channels)
            {
                if (string.IsNullOrWhiteSpace(channel)) continue;
                if (checkedChannels.Any(c => c.ToString() == channel)) continue;
                checkedChannels.Add(channel);

                var id = Setup.Constants.RegularExpressions.DiscordIdPattern.Match(channel).ToString();

                DiscordChannel channelToAdd;
                try
                {
                    channelToAdd = await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says that channel doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {channel} as a channel ID. Make sure it's formatted correctly! If you want to ignore multiple channels, please separate their IDs with a space.")
                        .AsEphemeral(true));
                    return;
                }

                channelsToIgnore.Add(channelToAdd.Id);
            }
        }
        thisKeywordData.ChannelIgnoreList = channelsToIgnore;

        List<string> checkedGuilds = [];
        List<ulong> guildsToIgnore = [];
        if (guildIgnoreList is not null)
        {
            var guilds = guildIgnoreList.Split(' ');
            foreach (var guild in guilds)
            {
                if (string.IsNullOrWhiteSpace(guild)) continue;
                if (checkedGuilds.Any(g => g.ToString() == guild)) continue;
                checkedGuilds.Add(guild);

                var id = Setup.Constants.RegularExpressions.DiscordIdPattern.Match(guild).ToString();

                DiscordGuild guildToAdd;
                try
                {
                    guildToAdd = await Setup.State.Discord.Client.GetGuildAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says that server doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral(true));
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {guild} as a server ID. Make sure it's formatted correctly! If you want to ignore multiple servers, please separate their IDs with a space.")
                        .AsEphemeral(true));
                    return;
                }

                guildsToIgnore.Add(guildToAdd.Id);
            }
        }
        thisKeywordData.GuildIgnoreList = guildsToIgnore;

        if (currentGuildOnly is not null)
            thisKeywordData.GuildId = currentGuildOnly.Value ? ctx.Guild.Id : default;

        // Write back to db
        await Setup.Storage.Redis.HashSetAsync("keywords", thisKeywordData.Id, JsonConvert.SerializeObject(thisKeywordData));

        // Respond
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("Done!").AsEphemeral(true));
    }

    [Command("list")]
    [Description("List tracked keywords.")]
    public static async Task TrackListCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(true);

        var keywords = (await Setup.Storage.Redis.HashGetAllAsync("keywords")).Select(x => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(x.Value));
        var keywordList = string.Join("\n", keywords.Select(k => $"- {k.Keyword.Truncate(45)}"));

        DiscordEmbedBuilder embed = new()
        {
            Title = "Tracked Keywords",
            Color = Setup.Constants.BotColor
        };

        if (string.IsNullOrWhiteSpace(keywordList))
            embed.WithDescription($"You don't have any tracked keywords! Add some with {CommandHelpers.GetSlashCmdMention("track add")}.");
        else
            embed.WithDescription($"**To see extended information, use {CommandHelpers.GetSlashCmdMention("track details")}.**\n" +
                      $"Keywords are truncated to 45 characters in this list.\n\n{keywordList}");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral(true));
    }

    [Command("details")]
    [Description("Show details about a tracked keyword.")]
    public static async Task TrackDetailsCommandAsync(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(Setup.Types.AutoCompleteProviders.TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to show details for.")] string keyword)
    {
        await ctx.DeferResponseAsync(true);

        var keywords = await Setup.Storage.Redis.HashGetAllAsync("keywords");

        var userKeywords = keywords.Select(field => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(field.Value))
            .Where(x => x.UserId == ctx.User.Id).ToList();

        if (userKeywords.Count == 0)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have any tracked keywords! Add some with {CommandHelpers.GetSlashCmdMention("track add")}."));

            return;
        }

        var keywordRawData = await Setup.Storage.Redis.HashGetAsync("keywords", keyword);

        if (string.IsNullOrWhiteSpace(keywordRawData))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that keyword! Please try again.").AsEphemeral(true));
            return;
        }

        var keywordData = JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(keywordRawData);
        var embed = await KeywordTrackingHelpers.GenerateKeywordDetailsEmbed(keywordData);
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral(true));
    }

    [Command("remove")]
    [Description("Untrack a keyword.")]
    public static async Task TrackRemoveCommandAsync(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(Setup.Types.AutoCompleteProviders.TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to untrack.")] string keyword)
    {
        await ctx.DeferResponseAsync(true);

        var keywords = await Setup.Storage.Redis.HashGetAllAsync("keywords");

        var userKeywords = keywords.Select(field => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(field.Value))
            .Where(x => x.UserId == ctx.User.Id).ToList();

        if (userKeywords.Count == 0)
        {
            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have any tracked keywords! Add some with {CommandHelpers.GetSlashCmdMention("track add")}."));

            return;
        }

        var keywordRawData = await Setup.Storage.Redis.HashGetAsync("keywords", keyword);

        if (string.IsNullOrWhiteSpace(keywordRawData))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that keyword! Please try again.").AsEphemeral(true));
            return;
        }

        var keywordData = JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(keywordRawData);
        var embed = (await KeywordTrackingHelpers.GenerateKeywordDetailsEmbed(keywordData))
            .WithTitle("Are you sure you want to remove this keyword?").WithColor(DiscordColor.Red)
            .WithDescription(keywordData.Keyword);

        DiscordButtonComponent confirmButton = new(DiscordButtonStyle.Danger, "button-callback-track-remove-confirm", "Remove");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AddActionRowComponent(confirmButton).AsEphemeral(true));
    }
}

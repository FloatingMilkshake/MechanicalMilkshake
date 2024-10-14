namespace MechanicalMilkshake.Commands;

[Command("track")]
[Description("Track or untrack keywords.")]
public partial class Track
{
    [Command("add")]
    [Description("Track a new keyword.")]
    [RequireGuild]
    public static async Task TrackAdd(SlashCommandContext ctx,
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
            guildIgnoreList.Split(' ').Any(g => g == ctx.Guild!.Id.ToString())) currentGuildOnly = false;

        var fields = await Program.Db.HashGetAllAsync("keywords");
        foreach (var field in fields)
        {
            var fieldValue = JsonConvert.DeserializeObject<TrackedKeyword>(field.Value);

            // If the keyword is already being tracked, delete the current entry
            // This way we don't end up with duplicate entries for keywords
            if (fieldValue!.Keyword != keyword) continue;
            await Program.Db.HashDeleteAsync("keywords", fieldValue.Id);
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

                var idRegex = UserIdPattern();
                var id = user.Contains('@') ? idRegex.Match(user).ToString() : user;

                DiscordUser userToAdd;
                try
                {
                    userToAdd = await Program.Discord.GetUserAsync(Convert.ToUInt64(id));
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {user} as a user ID. Make sure it's formatted correctly! If you want to ignore multiple users, separate their mentions or IDs with a space.")
                        .AsEphemeral());
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

                var idRegex = UserIdPattern();
                var id = idRegex.Match(channel).ToString();

                DiscordChannel channelToAdd;
                try
                {
                    channelToAdd = await Program.Discord.GetChannelAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says that channel doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {channel} as a channel ID. Make sure it's formatted correctly! If you want to ignore multiple channels, please separate their IDs with a space.")
                        .AsEphemeral());
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

                var idRegex = UserIdPattern();
                var id = idRegex.Match(guild).ToString();

                DiscordGuild guildToAdd;
                try
                {
                    guildToAdd = await Program.Discord.GetGuildAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says that server doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {guild} as a server ID. Make sure it's formatted correctly! If you want to ignore multiple servers, please separate their IDs with a space.")
                        .AsEphemeral());
                    return;
                }

                guildsToIgnore.Add(guildToAdd.Id);
            }
        }

        TrackedKeyword trackedKeyword = new()
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

        await Program.Db.HashSetAsync("keywords", ctx.Interaction.Id,
            JsonConvert.SerializeObject(trackedKeyword));
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Done!").AsEphemeral());
    }
    
    [Command("edit")]
    [Description("Edit a tracked keyword.")]
    public static async Task TrackEdit(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to edit.")]
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
        
        var allKeywordsRawData = await Program.Db.HashGetAllAsync("keywords");
        
        var userKeywords = allKeywordsRawData.Select(field => JsonConvert.DeserializeObject<TrackedKeyword>(field.Value))
            .Where(x => x!.UserId == ctx.User.Id).ToList();

        if (userKeywords.Count == 0)
        {
            var trackCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "track");

            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(trackCmd is null
                    ? "You don't have any tracked keywords!"
                    : $"You don't have any tracked keywords! Add some with </{trackCmd.Name} add:{trackCmd.Id}>."));

            return;
        }
        
        var thisKeywordRawData = allKeywordsRawData.FirstOrDefault(field =>
            JsonConvert.DeserializeObject<TrackedKeyword>(field.Value)!.Id.ToString() == keyword).Value;

        if (string.IsNullOrWhiteSpace(thisKeywordRawData))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that keyword! Please try again.").AsEphemeral());
            return;
        }
        
        if (newKeyword is null && matchWholeWord is null && ignoreBots is null && assumePresence is null &&
            userIgnoreList is null && channelIgnoreList is null && guildIgnoreList is null &&
            currentGuildOnly is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("You didn't change anything! Nothing to do.").AsEphemeral());
            return;
        }
        
        var thisKeywordData = JsonConvert.DeserializeObject<TrackedKeyword>(thisKeywordRawData.ToString());
        
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

                var idRegex = UserIdPattern();
                var id = user.Contains('@') ? idRegex.Match(user).ToString() : user;

                DiscordUser userToAdd;
                try
                {
                    userToAdd = await Program.Discord.GetUserAsync(Convert.ToUInt64(id));
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {user} as a user ID. Make sure it's formatted correctly! If you want to ignore multiple users, separate their mentions or IDs with a space.")
                        .AsEphemeral());
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

                var idRegex = UserIdPattern();
                var id = idRegex.Match(channel).ToString();

                DiscordChannel channelToAdd;
                try
                {
                    channelToAdd = await Program.Discord.GetChannelAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the channel {channel}! Discord says that channel doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that channel will still be respected." +
                            " However, the channel will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {channel} as a channel ID. Make sure it's formatted correctly! If you want to ignore multiple channels, please separate their IDs with a space.")
                        .AsEphemeral());
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

                var idRegex = UserIdPattern();
                var id = idRegex.Match(guild).ToString();

                DiscordGuild guildToAdd;
                try
                {
                    guildToAdd = await Program.Discord.GetGuildAsync(Convert.ToUInt64(id));
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says I'm not allowed to see it." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch (NotFoundException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                            $"I wasn't able to fetch the server {guild}! Discord says that server doesn't exist." +
                            " (I won't be able to track keywords there, so your decision to ignore that server will still be respected." +
                            " However, the server will not appear in this keyword's details.)")
                        .AsEphemeral());
                    continue;
                }
                catch
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"I wasn't able to parse {guild} as a server ID. Make sure it's formatted correctly! If you want to ignore multiple servers, please separate their IDs with a space.")
                        .AsEphemeral());
                    return;
                }

                guildsToIgnore.Add(guildToAdd.Id);
            }
        }
        thisKeywordData.GuildIgnoreList = guildsToIgnore;
        
        if (currentGuildOnly is not null)
            thisKeywordData.GuildId = currentGuildOnly.Value ? ctx.Guild.Id : default;
        
        // Write back to db
        await Program.Db.HashSetAsync("keywords", thisKeywordData.Id, JsonConvert.SerializeObject(thisKeywordData));
        
        // Respond
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("Done!").AsEphemeral());
    }

    [Command("list")]
    [Description("List tracked keywords.")]
    public static async Task TrackList(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(true);

        var data = await Program.Db.HashGetAllAsync("keywords");

        var response = data.Select(field => JsonConvert.DeserializeObject<TrackedKeyword>(field.Value))
            .Where(fieldValue => fieldValue!.UserId == ctx.User.Id).Aggregate("",
                (current, fieldValue) => current + $"- {fieldValue.Keyword.Truncate(45)}\n");

        var trackCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "track");

        DiscordEmbedBuilder embed = new()
        {
            Title = "Tracked Keywords",
            Color = Program.BotColor
        };

        if (string.IsNullOrWhiteSpace(response))
            embed.WithDescription(
                trackCmd is null
                    ? "You don't have any tracked keywords!"
                    : $"You don't have any tracked keywords! Add some with </{trackCmd.Name} add:{trackCmd.Id}>.");
        else
            embed.WithDescription(
                (trackCmd is null
                    ? ""
                    : $"**To see extended information, use </{trackCmd.Name} details:{trackCmd.Id}>.**\n" +
                      "Keywords are truncated to 45 characters in this list.\n\n")
                + response);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
    }

    [Command("details")]
    [Description("Show details about a tracked keyword.")]
    public static async Task TrackDetails(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to show details for.")] string keyword)
    {
        await ctx.DeferResponseAsync(true);

        var keywords = await Program.Db.HashGetAllAsync("keywords");

        var userKeywords = keywords.Select(field => JsonConvert.DeserializeObject<TrackedKeyword>(field.Value))
            .Where(x => x!.UserId == ctx.User.Id).ToList();

        if (userKeywords.Count == 0)
        {
            var trackCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "track");

            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                    trackCmd is null
                        ? "You don't have any tracked keywords!"
                        : $"You don't have any tracked keywords! Add some with </{trackCmd.Name} add:{trackCmd.Id}>."));

            return;
        }

        var keywordRawData = await Program.Db.HashGetAsync("keywords", keyword);
        
        if (string.IsNullOrWhiteSpace(keywordRawData))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that keyword! Please try again.").AsEphemeral());
            return;
        }
        
        var keywordData = JsonConvert.DeserializeObject<TrackedKeyword>(keywordRawData);
        var embed = await KeywordTrackingHelpers.GenerateKeywordDetailsEmbed(keywordData);
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
    }

    [Command("remove")]
    [Description("Untrack a keyword.")]
    public static async Task TrackRemove(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to untrack.")] string keyword)
    {
        await ctx.DeferResponseAsync(true);

        var keywords = await Program.Db.HashGetAllAsync("keywords");

        var userKeywords = keywords.Select(field => JsonConvert.DeserializeObject<TrackedKeyword>(field.Value))
            .Where(x => x!.UserId == ctx.User.Id).ToList();

        if (userKeywords.Count == 0)
        {
            var trackCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "track");

            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(trackCmd is null
                    ? "You don't have any tracked keywords!"
                    : $"You don't have any tracked keywords! Add some with </{trackCmd.Name} add:{trackCmd.Id}>."));

            return;
        }

        var keywordRawData = await Program.Db.HashGetAsync("keywords", keyword);
        
        if (string.IsNullOrWhiteSpace(keywordRawData))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that keyword! Please try again.").AsEphemeral());
            return;
        }
        
        var keywordData = JsonConvert.DeserializeObject<TrackedKeyword>(keywordRawData);
        var embed = (await KeywordTrackingHelpers.GenerateKeywordDetailsEmbed(keywordData))
            .WithTitle("Are you sure you want to remove this keyword?").WithColor(DiscordColor.Red)
            .WithDescription(keywordData!.Keyword);

        DiscordButtonComponent confirmButton = new(DiscordButtonStyle.Danger, "track-remove-confirm-button", "Remove");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AddComponents(confirmButton).AsEphemeral());
    }
    
    [GeneratedRegex("[0-9]+")]
    private static partial Regex UserIdPattern();
    
    private class TrackingAutocompleteProvider : IAutoCompleteProvider
    {
        public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
        {
            var allKeywordRawData = await Program.Db.HashGetAllAsync("keywords");
            var userKeywords = allKeywordRawData.Select(field => JsonConvert.DeserializeObject<TrackedKeyword>(field.Value))
                .Where(keyword => keyword!.UserId == ctx.User.Id).ToList();

            return userKeywords.Select(keyword => new DiscordAutoCompleteChoice(keyword.Keyword, keyword.Id.ToString())).ToList();
        }
    }
}
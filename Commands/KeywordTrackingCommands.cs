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
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

        if (currentGuildOnly && guildIgnoreList is not null &&
            guildIgnoreList.Split(' ').Any(g => g == ctx.Guild.Id.ToString()))
        {
            currentGuildOnly = false;
        }

        var allKeywords = (await Setup.Storage.Redis.HashGetAllAsync("keywords")).Select(k => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(k.Value));

        foreach (var matchingKeyword in allKeywords.Where(k => k.UserId == ctx.User.Id && k.Keyword == keyword))
        {
            await Setup.Storage.Redis.HashDeleteAsync("keywords", matchingKeyword.Id);
        }

        Setup.Types.TrackedKeyword trackedKeyword = new(
            keyword,
            ctx.User.Id,
            matchWholeWord,
            ignoreBots,
            assumePresence,
            await ParseUserIgnoreListAsync(userIgnoreList),
            await ParseChannelIgnoreListAsync(channelIgnoreList),
            await ParseGuildIgnoreListAsync(guildIgnoreList),
            ctx.Interaction.Id,
            currentGuildOnly ? ctx.Guild.Id : default);

        await Setup.Storage.Redis.HashSetAsync("keywords", ctx.Interaction.Id,
            JsonConvert.SerializeObject(trackedKeyword));
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Now tracking mentions of `{trackedKeyword.Keyword}`!")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
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
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

        var allKeywords = (await Setup.Storage.Redis.HashGetAllAsync("keywords")).Select(x =>
            JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(x.Value)).ToList();

        if (allKeywords.All(k => k.UserId != ctx.User.Id))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have any tracked keywords! Add some with {"track add".AsSlashCommandMention()}.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));

            return;
        }

        var thisKeyword = allKeywords.FirstOrDefault(k => k.UserId == ctx.User.Id && k.Id.ToString() == keyword);

        if (thisKeyword == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("I couldn't find that keyword! Please try again.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            return;
        }

        if (newKeyword is null && matchWholeWord is null && ignoreBots is null && assumePresence is null &&
            userIgnoreList is null && channelIgnoreList is null && guildIgnoreList is null &&
            currentGuildOnly is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("You didn't change anything! Nothing to do.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            return;
        }

        var newKeywordObject = new Setup.Types.TrackedKeyword(newKeyword is null ? thisKeyword.Keyword : newKeyword,
            ctx.User.Id,
            matchWholeWord is null ? thisKeyword.MatchWholeWord : matchWholeWord.Value,
            ignoreBots is null ? thisKeyword.IgnoreBots : ignoreBots.Value,
            assumePresence is null ? thisKeyword.AssumePresence : assumePresence.Value,
            await ParseUserIgnoreListAsync(userIgnoreList),
            await ParseChannelIgnoreListAsync(channelIgnoreList),
            await ParseGuildIgnoreListAsync(guildIgnoreList),
            thisKeyword.Id,
            currentGuildOnly is null ? default : currentGuildOnly.Value ? ctx.Guild.Id : default);

        await Setup.Storage.Redis.HashSetAsync("keywords", thisKeyword.Id, JsonConvert.SerializeObject(newKeywordObject));

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Successfully edited keyword `{newKeywordObject.Keyword}`!")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
    }

    [Command("list")]
    [Description("List tracked keywords.")]
    public static async Task TrackListCommandAsync(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

        var keywords = (await Setup.Storage.Redis.HashGetAllAsync("keywords")).Select(x => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(x.Value));
        var userKeywords = keywords.Where(k => k.UserId == ctx.User.Id);
        var keywordList = string.Join("\n", userKeywords.Select(k => $"- {k.Keyword.Truncate(45)}"));

        DiscordEmbedBuilder embed = new()
        {
            Title = "Tracked Keywords",
            Color = Setup.Constants.BotColor
        };

        if (string.IsNullOrWhiteSpace(keywordList))
            embed.WithDescription($"You don't have any tracked keywords! Add some with {"track add".AsSlashCommandMention()}.");
        else
            embed.WithDescription($"**To see extended information, use {"track details".AsSlashCommandMention()}.**\n" +
                      $"Keywords are truncated to 45 characters in this list.\n\n{keywordList}");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
    }

    [Command("details")]
    [Description("Show details about a tracked keyword.")]
    public static async Task TrackDetailsCommandAsync(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(Setup.Types.AutoCompleteProviders.TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to show details for.")] string keyword)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

        var allKeywords = (await Setup.Storage.Redis.HashGetAllAsync("keywords")).Select(k => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(k.Value));

        var userKeywords = allKeywords.Where(k => k.UserId == ctx.User.Id);

        if (!userKeywords.Any())
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have any tracked keywords! Add some with {"track add".AsSlashCommandMention()}."));

            return;
        }

        var thisKeyword = allKeywords.FirstOrDefault(k => k.UserId == ctx.User.Id && k.Id.ToString() == keyword);

        if (thisKeyword == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("I couldn't find that keyword! Please try again.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            return;
        }

        var embed = await thisKeyword.CreateDetailsEmbedAsync();
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
    }

    [Command("remove")]
    [Description("Untrack a keyword.")]
    public static async Task TrackRemoveCommandAsync(SlashCommandContext ctx,
        [SlashAutoCompleteProvider(typeof(Setup.Types.AutoCompleteProviders.TrackingAutocompleteProvider)), Parameter("keyword"), Description("The keyword or phrase to untrack.")] string keyword)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

        var allKeywords = (await Setup.Storage.Redis.HashGetAllAsync("keywords")).Select(k => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(k.Value));

        var userKeywords = allKeywords.Where(k => k.UserId == ctx.User.Id);

        if (!userKeywords.Any())
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have any tracked keywords! Add some with {"track add".AsSlashCommandMention()}.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));

            return;
        }

        var keywordToDelete = userKeywords.FirstOrDefault(k => k.Id.ToString() == keyword);

        if (keywordToDelete == default)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("I couldn't find that keyword! Please try again.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            return;
        }

        await Setup.Storage.Redis.HashDeleteAsync("keywords", keywordToDelete.Id);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"No longer tracking mentions of `{keywordToDelete.Keyword}`.")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
    }

    [Command("clear")]
    [Description("Clear your list of tracked keywords.")]
    public static async Task TrackClearCommandAsync(SlashCommandContext ctx)
    {
        await ctx.RespondAsync(new DiscordInteractionResponseBuilder()
            .WithContent("Are you sure you want to clear your list of tracked keywords? This cannot be undone!")
            .AddActionRowComponent([new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-track-clear-confirm", "Clear Tracked Keywords")])
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
    }

    private static async Task<List<ulong>> ParseUserIgnoreListAsync(string input)
    {
        List<string> checkedUsers = [];
        List<ulong> usersToIgnore = [];
        if (input is not null)
        {
            var users = input.Split(' ');
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
                catch (Exception e) when (e is FormatException or NotFoundException)
                {
                    // Couldn't parse or Discord returned an error, skip
                    continue;
                }

                usersToIgnore.Add(userToAdd.Id);
            }
        }

        return usersToIgnore;
    }

    private static async Task<List<ulong>> ParseChannelIgnoreListAsync(string input)
    {
        List<string> checkedChannels = [];
        List<ulong> channelsToIgnore = [];
        if (input is not null)
        {
            var channels = input.Split(' ');
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
                catch (Exception e) when (e is FormatException or NotFoundException)
                {
                    // Couldn't parse or Discord returned an error, skip
                    continue;
                }

                channelsToIgnore.Add(channelToAdd.Id);
            }
        }

        return channelsToIgnore;
    }

    private static async Task<List<ulong>> ParseGuildIgnoreListAsync(string input)
    {
        List<string> checkedGuilds = [];
        List<ulong> guildsToIgnore = [];
        if (input is not null)
        {
            var guilds = input.Split(' ');
            foreach (var guild in guilds)
            {
                if (string.IsNullOrWhiteSpace(guild)) continue;
                if (checkedGuilds.Any(g => g.ToString() == guild)) continue;
                checkedGuilds.Add(guild);

                var id = Setup.Constants.RegularExpressions.DiscordIdPattern.Match(guild).ToString();

                DiscordGuild guildToAdd;
                try
                {
                    guildToAdd = Setup.State.Discord.Client.Guilds[Convert.ToUInt64(id)];
                }
                catch (Exception e) when (e is FormatException or KeyNotFoundException)
                {
                    // Couldn't parse or Discord returned an error, skip
                    continue;
                }

                guildsToIgnore.Add(guildToAdd.Id);
            }
        }

        return guildsToIgnore;
    }
}

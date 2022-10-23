namespace MechanicalMilkshake.Commands;

public class KeywordTrackingCommands : ApplicationCommandModule
{
    [SlashCommandGroup("track", "Track or untrack keywords.")]
    public class Track
    {
        [SlashCommand("add", "Track a new keyword.")]
        public static async Task TrackAdd(InteractionContext ctx,
            [Option("keyword", "The keyword or phrase to track.")]
                string keyword,
            [Option("match_whole_word",
                "Whether you want to match the keyword only when it is a whole word. Defaults to False.")]
                bool matchWholeWord = false,
            [Option("ignore_bots", "Whether to ignore messages from bots. Defaults to True.")]
                bool ignoreBots = true,
            [Option("user_ignore_list", "Users to ignore. Use IDs and/or mentions. Separate with spaces.")]
                string userIgnoreList = null,
            [Option("channel_ignore_list", "Channels to ignore. Use IDs only. Separate with spaces.")]
                string channelIgnoreList = null,
            [Option("server_ignore_list", "Servers to ignore. Use IDs only. Separate with spaces.")]
                string guildIgnoreList = null,
            [Option("this_server_only",
                "Whether to only notify you if the keyword is mentioned in this server. Defaults to True.")]
                bool currentGuildOnly = true)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            if (currentGuildOnly && guildIgnoreList is not null &&
                guildIgnoreList.Split(' ').Any(g => g == ctx.Guild.Id.ToString())) currentGuildOnly = false;

            var fields = await Program.Db.HashGetAllAsync("keywords");
            foreach (var field in fields)
            {
                var fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);

                // If the keyword is already being tracked, delete the current entry
                // This way we don't end up with duplicate entries for keywords
                if (fieldValue!.Keyword != keyword) continue;
                await Program.Db.HashDeleteAsync("keywords", fieldValue.Id);
                break;
            }

            List<ulong> usersToIgnore = new();
            if (userIgnoreList is not null)
            {
                var users = userIgnoreList.Split(' ');
                foreach (var user in users)
                {
                    Regex idRegex = new("[0-9]+");
                    var id = user.Contains('@') ? idRegex.Match(user).ToString() : user;

                    DiscordUser userToAdd;
                    try
                    {
                        userToAdd = await Program.Discord.GetUserAsync(Convert.ToUInt64(id));
                    }
                    catch
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                            .WithContent(
                                $"I wasn't able to parse {user} as a user ID. Make sure it's formatted correctly! If you want to ignore multiple users, separate their mentions or IDs with a space.")
                            .AsEphemeral());
                        return;
                    }

                    usersToIgnore.Add(userToAdd.Id);
                }
            }

            List<ulong> channelsToIgnore = new();
            if (channelIgnoreList is not null)
            {
                var channels = channelIgnoreList.Split(' ');
                foreach (var channel in channels)
                {
                    Regex idRegex = new("[0-9]+");
                    var id = idRegex.Match(channel).ToString();

                    DiscordChannel channelToAdd;
                    try
                    {
                        channelToAdd = await Program.Discord.GetChannelAsync(Convert.ToUInt64(id));
                    }
                    catch
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                            .WithContent(
                                $"I wasn't able to parse {channel} as a channel ID. Make sure it's formatted correctly! If you want to ignore multiple channels, please separate their IDs with a space.")
                            .AsEphemeral());
                        return;
                    }

                    channelsToIgnore.Add(channelToAdd.Id);
                }
            }

            List<ulong> guildsToIgnore = new();
            if (guildIgnoreList is not null)
            {
                var guilds = guildIgnoreList.Split(' ');
                foreach (var guild in guilds)
                {
                    Regex idRegex = new("[0-9]+");
                    var id = idRegex.Match(guild).ToString();

                    DiscordGuild guildToAdd;
                    try
                    {
                        guildToAdd = await Program.Discord.GetGuildAsync(Convert.ToUInt64(id));
                    }
                    catch
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                            .WithContent(
                                $"I wasn't able to parse {guild} as a server ID. Make sure it's formatted correctly! If you want to ignore multiple servers, please separate their IDs with a space.")
                            .AsEphemeral());
                        return;
                    }

                    guildsToIgnore.Add(guildToAdd.Id);
                }
            }

            ulong guildId = default;
            if (currentGuildOnly) guildId = ctx.Guild.Id;

            KeywordConfig keywordConfig = new()
            {
                Keyword = keyword,
                UserId = ctx.User.Id,
                MatchWholeWord = matchWholeWord,
                IgnoreBots = ignoreBots,
                UserIgnoreList = usersToIgnore,
                ChannelIgnoreList = channelsToIgnore,
                GuildIgnoreList = guildsToIgnore,
                Id = ctx.InteractionId,
                GuildId = guildId
            };

            await Program.Db.HashSetAsync("keywords", ctx.InteractionId,
                JsonConvert.SerializeObject(keywordConfig));
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Done!").AsEphemeral());
        }

        [SlashCommand("list", "List tracked keywords.")]
        public static async Task TrackList(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var data = await Program.Db.HashGetAllAsync("keywords");

            var response = data.Select(field => JsonConvert.DeserializeObject<KeywordConfig>(field.Value))
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

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed).AsEphemeral());
        }

        [SlashCommand("details", "Show details about a tracked keyword.")]
        public static async Task TrackDetails(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var keywords = await Program.Db.HashGetAllAsync("keywords");

            var userKeywords = keywords.Select(field => JsonConvert.DeserializeObject<KeywordConfig>(field.Value))
                .Where(keyword => keyword!.UserId == ctx.User.Id).ToList();

            if (userKeywords.Count == 0)
            {
                var trackCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "track");

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        trackCmd is null
                            ? "You don't have any tracked keywords!"
                            : $"You don't have any tracked keywords! Add some with </{trackCmd.Name} add:{trackCmd.Id}>."));

                return;
            }

            var options = (from field in keywords
                select JsonConvert.DeserializeObject<KeywordConfig>(field.Value)
                into keyword
                where keyword!.UserId == ctx.User.Id
                select new DiscordSelectComponentOption(keyword.Keyword.Truncate(100), keyword.Id.ToString())).ToList();

            var dropdown =
                new DiscordSelectComponent("track-details-dropdown", null, options);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Please choose a keyword to see details for.").AddComponents(dropdown).AsEphemeral());
        }

        [SlashCommand("remove", "Untrack a keyword.")]
        public static async Task TrackRemove(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var keywords = await Program.Db.HashGetAllAsync("keywords");

            var userKeywords = keywords.Select(field => JsonConvert.DeserializeObject<KeywordConfig>(field.Value))
                .Where(keyword => keyword!.UserId == ctx.User.Id).ToList();

            if (userKeywords.Count == 0)
            {
                var trackCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "track");

                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(trackCmd is null
                        ? "You don't have any tracked keywords!"
                        : $"You don't have any tracked keywords! Add some with </{trackCmd.Name} add:{trackCmd.Id}>."));

                return;
            }

            var options = (from field in keywords
                select JsonConvert.DeserializeObject<KeywordConfig>(field.Value)
                into keyword
                where keyword!.UserId == ctx.User.Id
                select new DiscordSelectComponentOption(keyword.Keyword.Truncate(100), keyword.Id.ToString())).ToList();

            var dropdown =
                new DiscordSelectComponent("track-remove-dropdown", null, options);

            var untrackAllButton =
                new DiscordButtonComponent(ButtonStyle.Danger, "track-remove-all-button", "Remove All");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Please choose a keyword to stop tracking.").AddComponents(dropdown)
                .AddComponents(untrackAllButton));
        }
    }
}
namespace MechanicalMilkshake.Commands;

public class KeywordTracking : ApplicationCommandModule
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
            [Option("ignore_list", "Users to ignore. Use IDs and/or mentions. Separate with spaces.")]
            string ignoreList = null,
            [Option("this_server_only",
                "Whether to only notify you if the keyword is mentioned in this server. Defaults to True.")]
            bool currentGuildOnly = true)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var fields = await Program.db.HashGetAllAsync("keywords");
            foreach (var field in fields)
            {
                var fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);

                // If the keyword is already being tracked, delete the current entry
                // This way we don't end up with duplicate entries for keywords
                if (fieldValue.Keyword == keyword)
                {
                    await Program.db.HashDeleteAsync("keywords", fieldValue.Id);
                    break;
                }
            }

            List<ulong> usersToIgnore = new();
            if (ignoreList is not null)
            {
                var users = ignoreList.Split(' ');
                foreach (var user in users)
                {
                    string id;
                    // Mention
                    Regex idRegex = new("[0-9]+");
                    if (user.Contains('@'))
                        id = idRegex.Match(user).ToString();
                    // ID
                    else
                        id = user;

                    ulong userId;
                    DiscordUser userToAdd;
                    try
                    {
                        userId = Convert.ToUInt64(id);
                        userToAdd = await Program.discord.GetUserAsync(userId);
                    }
                    catch
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                            .WithContent(
                                $"I wasn't able to parse {user} as a user ID. Make sure it's formatted correctly! If you want to ignore multiple users, please separate their names, mentions or IDs with a `/`.")
                            .AsEphemeral());
                        return;
                    }

                    usersToIgnore.Add(userToAdd.Id);
                }
            }

            ulong guildId = default;
            if (currentGuildOnly)
            {
                guildId = ctx.Guild.Id;
            }

            KeywordConfig keywordConfig = new()
            {
                Keyword = keyword,
                UserId = ctx.User.Id,
                MatchWholeWord = matchWholeWord,
                IgnoreBots = ignoreBots,
                IgnoreList = usersToIgnore,
                Id = ctx.InteractionId,
                GuildId = guildId
            };

            await Program.db.HashSetAsync("keywords", ctx.InteractionId,
                JsonConvert.SerializeObject(keywordConfig));
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Tracking for \"{keyword}\" set up successfully.").AsEphemeral());
        }

        [SlashCommand("list", "List tracked keywords.")]
        public async Task TrackList(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var data = await Program.db.HashGetAllAsync("keywords");

            var response = "";
            foreach (var field in data)
            {
                var fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);

                if (fieldValue.UserId != ctx.User.Id)
                    continue;

                var ignoredUserMentions = "\n";
                foreach (var userToIgnore in fieldValue.IgnoreList)
                {
                    var user = await Program.discord.GetUserAsync(userToIgnore);
                    ignoredUserMentions += $"- {user.Mention}\n";
                }

                if (ignoredUserMentions == "\n") ignoredUserMentions = " None\n";

                var matchWholeWord = fieldValue.MatchWholeWord.ToString().Trim();

                string limitedGuild;
                if (fieldValue.GuildId == default)
                    limitedGuild = "None";
                else
                    limitedGuild = (await Program.discord.GetGuildAsync(fieldValue.GuildId)).Name;

                response += $"**{fieldValue.Keyword}**\n"
                            + $"Ignore Bots: {fieldValue.IgnoreBots}\n"
                            + $"Ignored Users:{ignoredUserMentions}"
                            + $"Match Whole Word: {matchWholeWord}\n"
                            + $"Limited to Server: {limitedGuild}\n\n";
            }

            if (string.IsNullOrWhiteSpace(response))
                response = "You don't have any tracked keywords! Add some with `/track add`.";

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response.Trim())
                .AsEphemeral());
        }

        [SlashCommand("remove", "Untrack a keyword.")]
        public async Task TrackRemove(InteractionContext ctx,
            [Option("keyword", "The keyword or phrase to remove.")]
            string keyword)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var data = await Program.db.HashGetAllAsync("keywords");
            var keywordReached = false;
            foreach (var field in data)
            {
                var keywordConfig = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);
                if (keywordConfig.UserId == ctx.User.Id && keywordConfig.Keyword == keyword)
                {
                    keywordReached = true;
                    await Program.db.HashDeleteAsync("keywords", keywordConfig.Id);
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"Tracked keyword \"{keyword}\" deleted successfully."));
                }
            }

            if (!keywordReached)
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You're not currently tracking that keyword!"));
        }
    }
}
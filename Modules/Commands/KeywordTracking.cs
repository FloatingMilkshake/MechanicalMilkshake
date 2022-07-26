namespace MechanicalMilkshake.Modules.Commands
{
    public class KeywordTracking : ApplicationCommandModule
    {
        [SlashCommandGroup("track", "Track or untrack keywords.")]
        public class Track
        {
            [SlashCommand("add", "Track a new keyword.")]
            public static async Task TrackAdd(InteractionContext ctx,
                [Option("keyword", "The keyword or phrase to track.")] string keyword,
                [Option("match_whole_word", "Whether you want to match the keyword only when it is a whole word. Defaults to False.")] bool matchWholeWord = false,
                [Option("ignore_bots", "Whether to ignore messages from bots. Defaults to True.")] bool ignoreBots = true,
                [Option("ignore_list", "Users to ignore. Use IDs and/or mentions. Separate with spaces.")] string ignoreList = null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                List<ulong> usersToIgnore = new();
                if (ignoreList is not null)
                {
                    string[] users = ignoreList.Split(' ');
                    foreach (string user in users)
                    {
                        string id;
                        // Mention
                        Regex idRegex = new("[0-9]+");
                        if (user.Contains('@'))
                        {
                            id = idRegex.Match(user).ToString();
                        }
                        // ID
                        else
                        {
                            id = user;
                        }

                        ulong userId;
                        DiscordUser userToAdd;
                        try
                        {
                            userId = Convert.ToUInt64(id);
                            userToAdd = await Program.discord.GetUserAsync(userId);
                        }
                        catch
                        {
                            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"I wasn't able to parse {user} as a user ID. Make sure it's formatted correctly! If you want to ignore multiple users, please separate their names, mentions or IDs with a `/`.").AsEphemeral(true));
                            return;
                        }
                        usersToIgnore.Add(userToAdd.Id);
                    }
                }

                KeywordConfig keywordConfig = new()
                {
                    Keyword = keyword,
                    UserId = ctx.User.Id,
                    MatchWholeWord = matchWholeWord,
                    IgnoreBots = ignoreBots,
                    IgnoreList = usersToIgnore,
                    Id = ctx.InteractionId
                };

                await Program.db.HashSetAsync("keywords", ctx.InteractionId, JsonConvert.SerializeObject(keywordConfig));
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Tracking for \"{keyword}\" set up successfully.").AsEphemeral(true));
            }

            [SlashCommand("list", "List tracked keywords.")]
            public async Task TrackList(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                HashEntry[] data = await Program.db.HashGetAllAsync("keywords");

                string response = "";
                foreach (HashEntry field in data)
                {
                    KeywordConfig fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);

                    if (fieldValue.UserId != ctx.User.Id)
                        continue;

                    string ignoredUserMentions = "\n";
                    foreach (ulong userToIgnore in fieldValue.IgnoreList)
                    {
                        DiscordUser user = await Program.discord.GetUserAsync(userToIgnore);
                        ignoredUserMentions += $"- {user.Mention}\n";
                    }

                    if (ignoredUserMentions == "\n")
                    {
                        ignoredUserMentions = " None\n";
                    }

                    string matchWholeWord = fieldValue.MatchWholeWord.ToString().Trim();

                    response += $"**{fieldValue.Keyword}**\n"
                        + $"Ignore Bots: {fieldValue.IgnoreBots}\n"
                        + $"Ignored Users:{ignoredUserMentions}"
                        + $"Match Whole Word: {matchWholeWord}\n\n";
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response.Trim()).AsEphemeral(true));
            }

            [SlashCommand("remove", "Untrack a keyword.")]
            public async Task TrackRemove(InteractionContext ctx, [Option("keyword", "The keyword or phrase to remove.")] string keyword)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                HashEntry[] data = await Program.db.HashGetAllAsync("reminders");
                foreach (HashEntry field in data)
                {
                    KeywordConfig keywordConfig = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);
                    if (keywordConfig.UserId == ctx.User.Id && keywordConfig.Keyword == keyword)
                    {
                        await Program.db.HashDeleteAsync("keywords", keywordConfig.Id);
                    }
                }
                await Program.db.HashDeleteAsync(ctx.User.Id.ToString(), keyword);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Tracked keyword \"{keyword}\" deleted successfully.").AsEphemeral(true));
            }
        }
    }

    public class KeywordConfig
    {
        [JsonProperty("keyword")]
        public string Keyword { get; set; }

        [JsonProperty("userId")]
        public ulong UserId { get; set; }

        [JsonProperty("matchWholeWord")]
        public bool MatchWholeWord { get; set; }

        [JsonProperty("ignoreBots")]
        public bool IgnoreBots { get; set; }

        [JsonProperty("ignoreList")]
        public List<ulong> IgnoreList { get; set; }

        [JsonProperty("id")]
        public ulong Id { get; set; }
    }
}

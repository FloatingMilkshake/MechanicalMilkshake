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
                [Option("ignore_bots", "Whether to ignore messages from bots. Defaults to True.")] bool ignoreBots = true,
                [Option("ignore_list", "Users to ignore. Use IDs and/or mentions. Separate with spaces.")] string ignoreList = null)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                List<ulong> usersToIgnore = new();
                if (ignoreList is not null)
                {
                    string[] users = ignoreList.Split(' ');
                    foreach (var user in users)
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
                    IgnoreBots = ignoreBots,
                    IgnoreList = usersToIgnore
                };

                await Program.db.HashSetAsync(ctx.User.Id.ToString(), keyword, JsonConvert.SerializeObject(keywordConfig));
                string response = $"Tracking for \"{keyword}\" set up successfully. ";
                if (ignoreBots)
                {
                    response += "Bots will be ignored.";
                }
                else
                {
                    response += "Bots will not be ignored, unless you specified any you would like to ignore specifically.";
                }
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response).AsEphemeral(true));
            }

            [SlashCommand("list", "List tracked keywords.")]
            public async Task TrackList(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                var data = await Program.db.HashGetAllAsync(ctx.User.Id.ToString());
                if (data.Length == 0)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You don't have any tracked keywords! Add some with `/track add`.").AsEphemeral(true));
                    return;
                }

                string response = "";
                foreach (var field in data)
                {
                    KeywordConfig fieldValue = JsonConvert.DeserializeObject<KeywordConfig>(field.Value);

                    string ignoredUserMentions = "\n";
                    foreach (var userToIgnore in fieldValue.IgnoreList)
                    {
                        DiscordUser user = await Program.discord.GetUserAsync(userToIgnore);
                        ignoredUserMentions += $"- {user.Mention}\n";
                    }

                    if (ignoredUserMentions == "\n")
                    {
                        ignoredUserMentions = " None\n";
                    }

                    response += $"**{field.Name}**\n"
                        + $"Ignore Bots: {fieldValue.IgnoreBots}\n"
                        + $"Ignored Users:{ignoredUserMentions}\n";
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response.Trim()).AsEphemeral(true));
            }

            [SlashCommand("remove", "Untrack a keyword.")]
            public async Task TrackRemove(InteractionContext ctx, [Option("keyword", "The keyword or phrase to remove.")] string keyword)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                await Program.db.HashDeleteAsync(ctx.User.Id.ToString(), keyword);

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Tracked keyword \"{keyword}\" deleted successfully.").AsEphemeral(true));
            }
        }
    }

    public class KeywordConfig
    {
        [JsonProperty("ignoreBots")]
        public bool IgnoreBots { get; set; }

        [JsonProperty("ignoreList")]
        public List<ulong> IgnoreList { get; set; }
    }
}

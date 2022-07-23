namespace MechanicalMilkshake.Modules.Commands
{
    public class Utility : ApplicationCommandModule
    {
        [SlashCommand("userinfo", "Returns information about the provided server member.")]
        [SlashRequireGuild]
        public async Task UserInfo(InteractionContext ctx, [Option("member", "The member to look up information for. Defaults to yourself if no member is provided.")] DiscordUser user = null)
        {
            DiscordMember member = null;

            if (user != null)
            {
                try
                {
                    member = await ctx.Guild.GetMemberAsync(user.Id);
                }
                catch
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Hmm. It doesn't look like that user is in the server, so I can't pull up their user info."));
                    return;
                }
            }
            else
            {
                user = ctx.User;
            }
            if (member == null)
            {
                member = ctx.Member;
            }

            ulong msSinceEpoch = member.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string registeredAt = ($"{msUnix / 1000}");

            TimeSpan t = member.JoinedAt - new DateTime(1970, 1, 1);
            int joinedAtTimestamp = (int)t.TotalSeconds;

            string acknowledgements = null;
            if (member.Permissions.HasPermission(Permissions.KickMembers) && member.Permissions.HasPermission(Permissions.BanMembers))
            {
                acknowledgements = "Server Moderator (can kick and ban members)";
            }
            if (member.Permissions.HasPermission(Permissions.Administrator))
            {
                acknowledgements = "Server Administrator";
            }
            if (member.IsOwner)
            {
                acknowledgements = "Server Owner";
            }

            string roles = "None";
            if (member.Roles.Any())
            {
                roles = "";
                foreach (DiscordRole role in member.Roles.OrderBy(role => role.Position).Reverse())
                {
                    roles += role.Mention + " ";
                }
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor($"{member.Color}"))
                .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                .AddField("User Mention", member.Mention)
                .AddField("User ID", $"{member.Id}")
                .AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)")
                .AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)")
                .AddField("Roles", roles)
                .WithThumbnail(member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow);

            if (acknowledgements != null)
            {
                embed.AddField("Acknowledgements", acknowledgements);
            }

            if (member.PremiumSince != null)
            {
                DateTime PremiumSinceUtc = member.PremiumSince.Value.UtcDateTime;
                long unixTime = ((DateTimeOffset)PremiumSinceUtc).ToUnixTimeSeconds();
                string boostingSince = $"Boosting since <t:{unixTime}:R> (<t:{unixTime}:F>";

                embed.AddField("Server Booster", boostingSince);
            }

            string badges = Helpers.GetBadges(user);
            if (badges != "")
            {
                embed.AddField("Badges", badges);
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"User Info for **{member.Username}#{member.Discriminator}**").AddEmbed(embed));
        }

        [SlashCommand("serverinfo", "Returns information about the server.")]
        [SlashRequireGuild]
        public async Task ServerInfo(InteractionContext ctx)
        {
            string description = "None";

            if (ctx.Guild.Description is not null)
            {
                description = ctx.Guild.Description;
            }

            ulong msSinceEpoch = ctx.Guild.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string createdAt = $"{msUnix / 1000}";

            DiscordMember botUserAsMember = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor($"{botUserAsMember.Color}"))
                .AddField("Server Owner", $"{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}", true)
                .AddField("Channels", $"{ctx.Guild.Channels.Count}", true)
                .AddField("Members", $"{ctx.Guild.MemberCount}", true)
                .AddField("Roles", $"{ctx.Guild.Roles.Count}", true)
                .WithThumbnail($"{ctx.Guild.IconUrl}")
                .AddField("Description", $"{description}", true)
                .WithFooter($"Server ID: {ctx.Guild.Id}")
                .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)", true);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Server Info for **{ctx.Guild.Name}**").AddEmbed(embed));
        }

        [SlashCommand("avatar", "Returns the avatar of the provided user. Defaults to yourself if no user is provided.")]
        public async Task Avatar(InteractionContext ctx, [Option("user", "The user whose avatar to get.")] DiscordUser user = null)
        {
            DiscordButtonComponent serverAvatarButton = new(ButtonStyle.Primary, "server-avatar-ctx-cmd-button", "Server Avatar");
            DiscordButtonComponent userAvatarButton = new(ButtonStyle.Primary, "user-avatar-ctx-cmd-button", "User Avatar");

            if (user == null)
            {
                user = ctx.User;
            }

            DiscordMember member = default;
            try
            {
                member = await ctx.Guild.GetMemberAsync(user.Id);
            }
            catch
            {
                // User is not in the server, so no guild avatar available
            }

            if (member == default || member.GuildAvatarUrl == null)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{user.AvatarUrl}".Replace("size=1024", "size=4096")));
            }
            else
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"You requested the avatar for {user.Mention}. Please choose one of the options below.").AddComponents(serverAvatarButton, userAvatarButton));
            }
        }

        [SlashCommandGroup("timestamp", "Returns the Unix timestamp of a given date.")]
        class TimestampCmds : ApplicationCommandModule
        {
            [SlashCommand("id", "Returns the Unix timestamp of a given Discord ID/snowflake.")]
            public async Task TimestampSnowflakeCmd(InteractionContext ctx, [Option("snowflake", "The ID/snowflake to fetch the Unix timestamp for.")] string id,
                [Choice("Short Time", "t")]
                [Choice("Long Time", "T")]
                [Choice("Short Date", "d")]
                [Choice("Long Date", "D")]
                [Choice("Short Date/Time", "f")]
                [Choice("Long Date/Time", "F")]
                [Choice("Relative Time", "R")]
                [Choice("Raw Timestamp", "")]
                [Option("format", "The format to convert the timestamp to.")] string format = "",
                [Option("includecode", "Whether to include the code for the timestamp.")] bool includeCode = false)
            {
                ulong snowflake;
                try
                {
                    snowflake = Convert.ToUInt64(id);
                }
                catch
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Hmm, that doesn't look like a valid ID/snowflake. I wasn't able to convert it to a timestamp."));
                    return;
                }

                ulong msSinceEpoch = snowflake >> 22;
                ulong msUnix = msSinceEpoch + 1420070400000;
                if (string.IsNullOrWhiteSpace(format))
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{msUnix / 1000}"));
                }
                else
                {
                    if (includeCode)
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"<t:{msUnix / 1000}:{format}> (`<t:{msUnix / 1000}:{format}>`)"));
                    }
                    else
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"<t:{msUnix / 1000}:{format}>"));
                    }
                }
            }

            [SlashCommand("date", "Returns the Unix timestamp of a given date.")]
            public async Task TimestampDateCmd(InteractionContext ctx, [Option("date", "The date to fetch the Unix timestamp for.")] string date,
                [Choice("Short Time", "t")]
                [Choice("Long Time", "T")]
                [Choice("Short Date", "d")]
                [Choice("Long Date", "D")]
                [Choice("Short Date/Time", "f")]
                [Choice("Long Date/Time", "F")]
                [Choice("Relative Time", "R")]
                [Choice("Raw Timestamp", "")]
                [Option("format", "The format to convert the timestamp to. Options are F/D/T/R/f/d/t.")] string format = "",
                [Option("includecode", "Whether to include the code for the timestamp.")] bool includeCode = false)
            {
                DateTime dateToConvert = Convert.ToDateTime(date);
                long unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();
                if (string.IsNullOrWhiteSpace(format))
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{unixTime}"));
                }
                else
                {
                    if (includeCode)
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"<t:{unixTime}:{format}> (`<t:{unixTime}:{format}>`)"));
                    }
                    else
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"<t:{unixTime}:{format}>"));
                    }
                }
            }
        }

        [SlashCommand("lookup", "Look up a user not in the current server.")]
        public async Task Lookup(InteractionContext ctx, [Option("user", "The user you want to look up.")] DiscordUser user)
        {
            ulong msSinceEpoch = user.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string createdAt = ($"{msUnix / 1000}");

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithThumbnail($"{user.AvatarUrl}")
                .AddField("ID", $"{user.Id}")
                .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

            string badges = Helpers.GetBadges(user);
            if (badges != "")
            {
                embed.AddField("Badges", badges);
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Information about **{user.Username}#{user.Discriminator}**:").AddEmbed(embed));
        }

        [SlashCommand("markdown", "Expose the Markdown formatting behind a message!")]
        public async Task Markdown(InteractionContext ctx, [Option("message", "The message you want to expose the formatting of. Accepts message IDs and links.")] string messageToExpose)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordMessage message = null;
            if (!messageToExpose.Contains("discord.com"))
            {
                ulong messageId;
                try
                {
                    messageId = Convert.ToUInt64(messageToExpose);
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Hmm, that doesn't look like a valid message ID or link. I wasn't able to get the Markdown data from it."));
                    return;
                }

                message = await ctx.Channel.GetMessageAsync(messageId);
            }
            else
            {
                // Assume the user provided a message link. Extract channel and message IDs to get message content.

                // Extract all IDs from URL. This will leave you with something like "guild_id/channel_id/message_id".
                Regex extractId = new(@".*.discord.com\/channels\/");
                Match selectionToRemove = extractId.Match(messageToExpose);
                messageToExpose = messageToExpose.Replace(selectionToRemove.ToString(), "");

                // Extract channel ID. This will leave you with "/channel_id".
                Regex getChannelId = new(@"\/[a-zA-Z0-9]*");
                Match channelId = getChannelId.Match(messageToExpose);
                // Remove '/' to get "channel_id"
                ulong targetChannelId = Convert.ToUInt64(channelId.ToString().Replace("/", ""));

                DiscordChannel channel;
                try
                {
                    channel = await ctx.Client.GetChannelAsync(targetChannelId);
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("I wasn't able to find that message! Make sure I have permission to see the channel it's in."));
                    return;
                }

                // Now we have the channel ID and need to get the message inside that channel. To do this we'll need the message ID from what we had before...

                Regex getMessageId = new(@"[a-zA-Z0-9]*\/[a-zA-Z0-9]*\/");
                Match idsToRemove = getMessageId.Match(messageToExpose);
                string targetMsgId = messageToExpose.Replace(idsToRemove.ToString(), "");

                ulong targetMessage = Convert.ToUInt64(targetMsgId.ToString());

                try
                {
                    message = await channel.GetMessageAsync(targetMessage);
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("I wasn't able to read that message! Make sure I have permisison to access it."));
                }
            }


            string msgContentEscaped = message.Content.Replace("`", @"\`");
            msgContentEscaped = msgContentEscaped.Replace("*", @"\*");
            msgContentEscaped = msgContentEscaped.Replace("_", @"\_");
            msgContentEscaped = msgContentEscaped.Replace("~", @"\~");
            msgContentEscaped = msgContentEscaped.Replace(">", @"\>");
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"{msgContentEscaped}"));
        }

        [SlashCommand("ping", "Checks my ping.")]
        public async Task Ping(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Pong! Client ping is `{ctx.Client.Ping}ms`.\n\nChecking round-trip time..."));

            DiscordMessage message;
            try
            {
                message = await ctx.Channel.SendMessageAsync("Pong! This is a temporary message used to check ping and should be deleted shortly.");
            }
            catch
            {
                // Round-trip ping failed because the bot doesn't have permission to send messages. That's fine though, we can still return client ping.

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Pong! Client ping is `{ctx.Client.Ping}ms`.\n\nI tried to send a message to check round-trip ping, but I don't have permission to send messages in this channel! Try again in another channel where I have permission to send messages."));
                return;
            }

            ulong msSinceEpoch = message.Id >> 22;
            ulong messageTimestamp = msSinceEpoch + 1420070400000;
            DateTimeOffset messageTimestampOffset = DateTimeOffset.FromUnixTimeMilliseconds((long)messageTimestamp);
            DateTime messageTimestampDateTime = messageTimestampOffset.UtcDateTime;


            string responseTime = (messageTimestampDateTime - ctx.Interaction.CreationTimestamp.UtcDateTime).ToString()
                .Replace("0", "")
                .Replace(":", "")
                .Replace(".", "");

            await message.DeleteAsync();

            await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent($"Pong! Client ping is `{ctx.Client.Ping}ms`.\n\nIt took me `{responseTime}ms` to send a message after you used this command."));
        }

        [SlashCommand("wolframalpha", "Search WolframAlpha without leaving Discord!")]
        public async Task WolframAlpha(InteractionContext ctx, [Option("query", "What to search for.")] string query,
            [Option("responsetype", "Whether the response should be simple text only or a more-detailed image. Defaults to Text.")]
            [Choice("Text", "text")]
            [Choice("Image", "image")] string responseType = "text")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            string queryEncoded;
            if (query == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Hmm, it doesn't look like you entered a valid query. Try something like `/wolframalpha query:What is the meaning of life?`."));
                return;
            }
            else
            {
                queryEncoded = HttpUtility.UrlEncode(query);
            }

            string appid;
            if (Program.configjson.WolframAlphaAppId == null)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Looks like you don't have an App ID! Check the wolframAlphaAppId field in your config.json file. "
                    + "If you don't know how to get an App ID, see Getting Started here: <https://products.wolframalpha.com/short-answers-api/documentation/>"));
                return;
            }
            else
            {
                appid = Program.configjson.WolframAlphaAppId;
            }

            string queryEscaped = query.Replace("`", @"\`");
            queryEscaped = queryEscaped.Replace("*", @"\*");
            queryEscaped = queryEscaped.Replace("_", @"\_");
            queryEscaped = queryEscaped.Replace("~", @"\~");
            queryEscaped = queryEscaped.Replace(">", @"\>");

            if (responseType == "text")
            {
                try
                {
                    string data = await Program.httpClient.GetStringAsync($"https://api.wolframalpha.com/v1/result?appid={appid}&i={query}");
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"> {queryEscaped}\n" + data + $"\n\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)"));
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Something went wrong while searching WolframAlpha and I couldn't get a simple answer for your query! You might have better luck if you set `responsetype` to `Image`.\n\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)"));
                }
            }
            else
            {
                try
                {
                    byte[] data = await Program.httpClient.GetByteArrayAsync($"https://api.wolframalpha.com/v1/simple?appid={appid}&i={query}");
                    await File.WriteAllBytesAsync("result.gif", data);

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"> {queryEscaped}\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)").AddFile(File.OpenRead("result.gif")));
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Something went wrong while searching WolframAlpha and I couldn't get an image response for your query! You might have better luck if you set `responsetype` to `Text`.\n\n[Query URL](<https://www.wolframalpha.com/input/?i={queryEncoded}>)"));
                }
            }
        }

        [SlashCommand("charactercount", "Counts the characters in a message.")]
        public async Task CharacterCount(InteractionContext ctx, [Option("message", "The message to count the characters of.")] string chars)
        {
            int count = 0;
            foreach (char chr in chars)
            {
                count++;
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(count.ToString()));
        }

        [SlashCommand("stealemoji", "Fetch all of a server's emoji! Note that the bot must be in the server for this to work.")]
        public async Task StealEmoji(InteractionContext ctx, [Option("server", "The ID of the server to fetch emoji from.")] string server, [Option("addtoserver", "Whether to add all of the emoji to the server you're running the command in.")] bool addToServer = false)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            ulong guildId;
            DiscordGuild guild;
            try
            {
                guildId = Convert.ToUInt64(server);
                guild = await ctx.Client.GetGuildAsync(guildId);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"I was able to find that server, but I don't have access to its emoji! The most likely reason for this is that I am not in that server; I cannot fetch emoji from a server I am not in. If you think I am in the server and you're still seeing this, contact the bot owner for help."));
                return;
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"I couldn't find that server! Make sure `{server}` is a server ID. If you're sure it is and you're still seeing this, contact the bot owner for help."));
                return;
            }

            string response = $"Emoji for **{guild.Name}**\n\n";

            string staticEmoji = "";
            string animatedEmoji = "";

            bool copySuccess = false;
            bool copyFail = false;

            foreach (KeyValuePair<ulong, DiscordEmoji> emoji in guild.Emojis)
            {
                if (emoji.Value.IsAnimated)
                {
                    animatedEmoji += $" <a:{emoji.Value.Name}:{emoji.Key}>";
                }
                else
                {
                    staticEmoji += $" <:{emoji.Value.Name}:{emoji.Key}>";
                }

                if (addToServer)
                {
                    if (!ctx.Member.Permissions.HasPermission(Permissions.ManageEmojis) || !ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.ManageEmojis))
                    {
                        copyFail = true;
                    }

                    Stream stream = await Program.httpClient.GetStreamAsync(emoji.Value.Url);
                    using (var ms = new MemoryStream())
                    {
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        await ctx.Guild.CreateEmojiAsync(emoji.Value.Name, ms, null, $"Emoji copied from {guild.Id} with /{ctx.CommandName} by {ctx.User.Id}.");

                    }
                    copySuccess = true;
                }
            }

            if (staticEmoji != "")
            {
                response += $"Static Emoji:\n{staticEmoji}";
            }
            if (animatedEmoji != "")
            {
                response += $"Animated Emoji:\n{animatedEmoji}";
            }

            if (copySuccess)
            {
                response += "\n\nThese emoji have been copied to this server.";
            }
            if (copyFail)
            {
                response += "\n\nI couldn't copy these emoji to this server! Make sure you and I both have the \"Manage Emojis and Stickers\" permission.";
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
        }

        [SlashCommand("bigemoji", "Enlarge an emoji! Only works for custom emoji.")]
        public async Task BigEmoji(InteractionContext ctx, [Option("emoji", "The emoji to enlarge.")] string emoji)
        {
            Regex emojiRegex = new(@"<(a)?:.*:([0-9]*)>");

            MatchCollection matches = emojiRegex.Matches(emoji);
            GroupCollection groups = matches[0].Groups;

            if (groups[1].Value == "a")
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"https://cdn.discordapp.com/emojis/{groups[2].Value}.gif"));
            }
            else
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"https://cdn.discordapp.com/emojis/{groups[2].Value}"));
            }
        }

        [SlashCommand("about", "View information about the bot!")]
        public async Task About(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = $"About {ctx.Client.CurrentUser.Username}",
                Description = $"A multipurpose bot with many miscellaneous commands. Type `/` and select {ctx.Client.CurrentUser.Username} to see available commands.",
                Color = new DiscordColor("#FAA61A")
            };
            embed.AddField("Servers", ctx.Client.Guilds.Count.ToString(), true);
            embed.AddField("Total User Count (not unique)", ctx.Client.Guilds.Sum(g => g.Value.MemberCount).ToString(), true);

            // Unique user count
            List<DiscordUser> uniqueUsers = new();
            foreach (KeyValuePair<ulong, DiscordGuild> guild in ctx.Client.Guilds)
            {
                foreach (KeyValuePair<ulong, DiscordMember> member in guild.Value.Members)
                {
                    DiscordUser user = await ctx.Client.GetUserAsync(member.Value.Id);
                    if (!uniqueUsers.Contains(user))
                    {
                        uniqueUsers.Add(user);
                    }
                }
            }
            embed.AddField("Unique Users", uniqueUsers.Count.ToString(), true);

            // Commit hash / version
            string commitHash = "";
            if (File.Exists("CommitHash.txt"))
            {
                StreamReader readHash = new("CommitHash.txt");
                commitHash = readHash.ReadToEnd().Trim();
            }
            if (commitHash == "")
            {
                commitHash = "dev";
            }

            string remoteUrl = "";
            string commitUrl = "";
            if (File.Exists("RemoteUrl.txt"))
            {
                StreamReader readUrl = new("RemoteUrl.txt");
                remoteUrl = $"{readUrl.ReadToEnd().Trim()}";
                commitUrl = $"{remoteUrl}/commit/{commitHash}";
            }
            if (remoteUrl == "")
            {
                remoteUrl = "N/A";
            }
            if (commitUrl == "")
            {
                commitUrl = "N/A";
            }
            embed.AddField("Version", $"[{commitHash}]({commitUrl})", true);
            embed.AddField("Source Code Repository", remoteUrl, true);

            List<DiscordUser> botOwners = new();
            List<DiscordUser> authorizedUsers = new();

            foreach (DiscordUser owner in ctx.Client.CurrentApplication.Owners)
            {
                botOwners.Add(owner);
            }
            foreach (string userId in Program.configjson.AuthorizedUsers)
            {
                authorizedUsers.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));
            }

            string ownerPhrasing;
            if (botOwners.Count > 1)
            {
                ownerPhrasing = "s are";
            }
            else
            {
                ownerPhrasing = " is";
            }

            List<string> botOwnerNames = new();
            foreach (DiscordUser owner in botOwners)
            {
                botOwnerNames.Add($"{owner.Username}#{owner.Discriminator}");
            }

            List<string> authorizedUserNames = new();
            foreach (DiscordUser user in authorizedUsers)
            {
                authorizedUserNames.Add($"{user.Username}#{user.Discriminator}");
            }

            embed.AddField("Owners", $"Bot owner{ownerPhrasing} {string.Join(", ", botOwnerNames)}.\n" +
                $"Users authorized to use owner-level commands are: {string.Join(", ", authorizedUserNames)}\n\nFor any issues with the bot, DM it or a __bot owner__.", false);

            DateTime startTime = Convert.ToDateTime(Program.processStartTime);
            long startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();
            embed.AddField("Uptime", $"Up since <t:{startUnixTime}:F> (<t:{startUnixTime}:R>!)", false);

            embed.WithFooter($"Using DSharpPlus {Program.discord.VersionString} and {RuntimeInformation.FrameworkDescription}");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        // Begin context menu commands

        [ContextMenu(ApplicationCommandType.UserContextMenu, "User Info")]
        [SlashRequireGuild]
        public async Task ContextUserInfo(ContextMenuContext ctx)
        {
            DiscordMember member = null;

            try
            {
                member = await ctx.Guild.GetMemberAsync(ctx.TargetUser.Id);
            }
            catch
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Hmm. It doesn't look like that user is in the server, so I can't pull up their user info.").AsEphemeral(true));
                return;
            }

            ulong msSinceEpoch = member.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string registeredAt = ($"{msUnix / 1000}");

            TimeSpan t = member.JoinedAt - new DateTime(1970, 1, 1);
            int joinedAtTimestamp = (int)t.TotalSeconds;

            string acknowledgements = null;
            if (member.Permissions.HasPermission(Permissions.KickMembers) && member.Permissions.HasPermission(Permissions.BanMembers))
            {
                acknowledgements = "Server Moderator (can kick and ban members)";
            }
            if (member.Permissions.HasPermission(Permissions.Administrator))
            {
                acknowledgements = "Server Administrator";
            }
            if (member.IsOwner)
            {
                acknowledgements = "Server Owner";
            }

            string roles = "None";
            if (member.Roles.Any())
            {
                roles = "";
                foreach (DiscordRole role in member.Roles.OrderBy(role => role.Position).Reverse())
                {
                    roles += role.Mention + " ";
                }
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor($"{member.Color}"))
                .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                .AddField("User Mention", member.Mention)
                .AddField("User ID", $"{member.Id}")
                .AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)")
                .AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)")
                .AddField("Roles", roles)
                .WithThumbnail(member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow);

            if (acknowledgements != null)
            {
                embed.AddField("Acknowledgements", acknowledgements);
            }

            if (member.PremiumSince != null)
            {
                DateTime PremiumSinceUtc = member.PremiumSince.Value.UtcDateTime;
                long unixTime = ((DateTimeOffset)PremiumSinceUtc).ToUnixTimeSeconds();
                string boostingSince = $"Boosting since <t:{unixTime}:R> (<t:{unixTime}:F>";

                embed.AddField("Server Booster", boostingSince);
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"User Info for **{member.Username}#{member.Discriminator}**").AddEmbed(embed).AsEphemeral(true));
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Avatar")]
        public async Task ContextAvatar(ContextMenuContext ctx)
        {
            DiscordButtonComponent serverAvatarButton = new(ButtonStyle.Primary, "server-avatar-ctx-cmd-button", "Server Avatar");
            DiscordButtonComponent userAvatarButton = new(ButtonStyle.Primary, "user-avatar-ctx-cmd-button", "User Avatar");

            DiscordMember member = default;
            try
            {
                member = await ctx.Guild.GetMemberAsync(ctx.TargetUser.Id);
            }
            catch
            {
                // User is not in the server, so no guild avatar available
            }

            if (member == default || member.GuildAvatarUrl == null)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{ctx.TargetUser.AvatarUrl}".Replace("size=1024", "size=4096")).AsEphemeral(true));
            }
            else
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"You requested the avatar for {ctx.TargetUser.Mention}. Please choose one of the options below.").AddComponents(serverAvatarButton, userAvatarButton).AsEphemeral(true));
            }
        }

        [ContextMenu(ApplicationCommandType.UserContextMenu, "Lookup")]
        public async Task ContextLookup(ContextMenuContext ctx)
        {
            ulong msSinceEpoch = ctx.TargetUser.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string createdAt = ($"{msUnix / 1000}");

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithThumbnail($"{ctx.TargetUser.AvatarUrl}")
                .AddField("ID", $"{ctx.TargetUser.Id}")
                .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Information about **{ctx.TargetUser.Username}#{ctx.TargetUser.Discriminator}**:").AddEmbed(embed).AsEphemeral(true));
        }
    }
}

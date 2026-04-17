using static MechanicalMilkshake.Setup.Types;

namespace MechanicalMilkshake.Setup;

internal static class Types
{
    internal class ConfigJson
    {
        [JsonProperty("botToken")] internal string BotToken { get; private set; }

        [JsonProperty("homeChannel")] internal string HomeChannel { get; private set; }

        [JsonProperty("homeServer")] internal string HomeServer { get; private set; }

        [JsonProperty("wolframAlphaAppId")] internal string WolframAlphaAppId { get; private set; }

        [JsonProperty("botCommanders")] internal string[] BotCommanders { get; private set; }

        [JsonProperty("useServerSpecificFeatures")] internal bool UseServerSpecificFeatures { get; private set; }

        [JsonProperty("uptimeKumaHeartbeatUrl")] internal string UptimeKumaHeartbeatUrl { get; private set; }

        [JsonProperty("feedbackChannel")] internal string FeedbackChannel { get; private set; }

        [JsonProperty("ratelimitCautionChannels")] internal List<string> RatelimitCautionChannels { get; private set; }

        [JsonProperty("slashCommandLogChannel")] internal string SlashCommandLogChannel { get; set; }

        [JsonProperty("slashCommandLogExcludedGuilds")] internal string[] SlashCommandLogExcludedGuilds { get; set; }

        [JsonProperty("guildLogChannel")] internal string GuildLogChannel { get; private set; }

        [JsonProperty("doDbotsStatsPosting")] internal bool DoDbotsStatsPosting { get; private set; }

        [JsonProperty("dbotsApiToken")] internal string DbotsApiToken { get; private set; }
    }

    internal class DebugInfo
    {
        internal string DotNetVersion { get; private set; }
        internal string OperatingSystem { get; private set; }
        internal string DSharpPlusVersion { get; private set; }
        internal string CommitInformation { get; private set; }

        private DebugInfo(
            string dotnetVersion,
            string operatingSystem,
            string DSharpPlusVersion,
            string commitInformation)
        {
            DotNetVersion = dotnetVersion;
            OperatingSystem = operatingSystem;
            this.DSharpPlusVersion = DSharpPlusVersion;
            CommitInformation = commitInformation;
        }

        private DebugInfo() { }

        internal static async Task<DebugInfo> GetDebugInfoAsync()
        {
            return new DebugInfo(
                dotnetVersion: RuntimeInformation.FrameworkDescription.Replace(".NET", "").Trim(),
                operatingSystem: RuntimeInformation.OSDescription,
                DSharpPlusVersion: Setup.State.Discord.Client.VersionString.Split('+').First(),
                commitInformation: await GetCommitInformationAsync()
            );
        }

        private static async Task<string> GetCommitInformationAsync()
        {
#if DEBUG
            return "dev";
#else
            var commitHash = await File.ReadAllTextOrFallbackAsync("CommitHash.txt");
            var commitTimestamp = $"<t:{Convert.ToDateTime(await File.ReadAllTextOrFallbackAsync("CommitTime.txt")).ToUnixTimeSeconds()}:F>";
            var commitMessage = await File.ReadAllTextOrFallbackAsync("CommitMessage.txt");
            var remoteUrl = await File.ReadAllTextOrFallbackAsync("RemoteUrl.txt");

            return $"[`{commitHash}`]({remoteUrl}/commit/{commitHash}): {commitMessage}\n{commitTimestamp}";
#endif
        }

        internal static async Task<DiscordEmbedBuilder> CreateDebugInfoEmbedAsync(bool isOnStartup)
        {
            // Check whether GuildDownloadCompleted has been fired yet
            // If not, wait until it has
            while (!Setup.State.Discord.GuildDownloadCompleted)
                await Task.Delay(1000);

            var debugInfo = await GetDebugInfoAsync();

            return new DiscordEmbedBuilder()
            {
                Title = isOnStartup ? "Connected!" : null,
                Color = Setup.Constants.BotColor
            }
            .AddField(".NET Version", debugInfo.DotNetVersion, true)
            .AddField("Operating System", debugInfo.OperatingSystem, true)
            .AddField("DSharpPlus Version", debugInfo.DSharpPlusVersion, true)
            .AddField("Version", debugInfo.CommitInformation, false);
        }
    }

    internal class AutoCompleteProviders
    {
        internal class TrackingAutocompleteProvider : IAutoCompleteProvider
        {
            public async ValueTask<IEnumerable<DiscordAutoCompleteChoice>> AutoCompleteAsync(AutoCompleteContext ctx)
            {
                var allKeywordRawData = await Setup.Storage.Redis.HashGetAllAsync("keywords");
                var userKeywords = allKeywordRawData.Select(field => JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(field.Value))
                    .Where(keyword => keyword.UserId == ctx.User.Id).ToList();

                var focusedOption = ctx.Options.FirstOrDefault(x => x.Focused);

                if (focusedOption is not null)
                {
                    return userKeywords.Where(k => k.Keyword.Contains(focusedOption.Value.ToString()))
                        .Select(keyword => new DiscordAutoCompleteChoice(keyword.Keyword, keyword.Id.ToString())).ToList();
                }
                return default;
            }
        }
    }

    internal static class ChoiceProviders
    {
        internal class GuildsListSortChoiceProvider : IChoiceProvider
        {
            private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
            [
                new("Name", "name"),
                new("Join Date", "joinDate"),
            ];

            public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
        }

        internal class GuildsListSortDirectionChoiceProvider : IChoiceProvider
        {
            private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
            [
                new("Ascending", "asc"),
                new("Descending", "desc"),
            ];

            public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
        }

        internal class ChecksChoiceProvider : IChoiceProvider
        {
            private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
            [
                new("All", "all"),
                new("Reminders", "reminders"),
                new("Redis Connection", "redisConnection")
            ];

            public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
        }

        internal class TestExceptionChoiceProvider : IChoiceProvider
        {
            private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
            [
                new("NullReferenceException", "nullref"),
                new("InvalidOperationException", "invalidop"),
                new("ChecksFailedException", "checksfailed")
            ];

            public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
        }

        internal class TimestampFormatChoiceProvider : IChoiceProvider
        {
            private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
            [
                new("Short Time", "t"),
                new("Long Time", "T"),
                new("Short Date", "d"),
                new("Long Date", "D"),
                new("Short Date/Time", "f"),
                new("Long Date/Time", "F"),
                new("Relative Time", "R"),
                new("Raw Timestamp", "")
            ];

            public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
        }
    }

    internal static class MessageCaching
    {
        internal class CachedMessage
        {
            internal ulong ChannelId { get; private set; }
            internal ulong MessageId { get; private set; }
            internal ulong AuthorId { get; private set; }

            internal CachedMessage(ulong channelId, ulong messageId, ulong authorId)
            {
                ChannelId = channelId;
                MessageId = messageId;
                AuthorId = authorId;
            }

            private CachedMessage() { }
        }

        internal class MessageCache
        {
            // Constructors

            // Init cache with list
            internal MessageCache(List<CachedMessage> messages)
            {
                Messages = messages;
            }

            // Init new/empty cache
            internal MessageCache()
            {
                Messages = [];
            }

            // Get data from cache

            // Equivalent to List<T>().TryGetValue(TKey, out TValue)
            internal bool TryGetMessage(ulong messageId, out CachedMessage message)
            {
                message = GetMessage(messageId);
                return message != null;
            }

            // Same as above but checking by channel ID
            internal bool TryGetMessageByChannel(ulong channelId, out CachedMessage message)
            {
                message = GetMessageByChannel(channelId);
                return message != null;
            }

            // Same as above but checking by author ID
            internal bool TryGetMessageByAuthor(ulong authorId, out CachedMessage message)
            {
                message = GetMessageByAuthor(authorId);
                return message != null;
            }

            // Get message by ID
            internal CachedMessage GetMessage(ulong messageId)
            {
                return Messages.Find(x => x.MessageId == messageId);
            }

            // Get message by chan ID
            internal CachedMessage GetMessageByChannel(ulong channelId)
            {
                return Messages.Find(x => x.ChannelId == channelId);
            }

            // Get message by author ID
            internal CachedMessage GetMessageByAuthor(ulong authorId)
            {
                return Messages.Find(x => x.AuthorId == authorId);
            }

            // Get entire cache
            internal List<CachedMessage> GetAllMessages()
            {
                return Messages;
            }

            // Get count of messages in cache
            internal int Count()
            {
                return Messages.Count;
            }

            // Get count of messages in cache with filter
            internal int Count(Func<CachedMessage, bool> predicate)
            {
                return Messages.Count(predicate);
            }

            // Modify cache

            // Add message to cache
            internal void AddMessage(CachedMessage message)
            {
                if (TryGetMessageByChannel(message.ChannelId, out var _))
                    RemoveChannel(message.ChannelId);

                Messages.Add(message);
            }

            // Remove message from cache by message ID
            internal void RemoveMessage(ulong messageId)
            {
                Messages.RemoveAll(x => x.MessageId == messageId);
            }

            // Remove message from cache by chan ID
            internal void RemoveChannel(ulong channelId)
            {
                Messages.RemoveAll(x => x.ChannelId == channelId);
            }

            // Remove message from cache by author ID
            internal void RemoveAuthor(ulong authorId)
            {
                Messages.RemoveAll(x => x.AuthorId == authorId);
            }

            private List<CachedMessage> Messages { get; }
        }
    }

    internal class TrackedKeyword
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

        private TrackedKeyword() { }

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

    internal class Reminder
    {
        [JsonProperty("userId")] internal ulong UserId { get; private set; }

        [JsonProperty("channelId")] internal ulong ChannelId { get; private set; }

        [JsonProperty("guildId")] internal string GuildId { get; private set; }

        [JsonProperty("messageId")] internal ulong MessageId { get; private set; }

        [JsonProperty("reminderId")] internal int ReminderId { get; private set; }

        [JsonProperty("reminderText")] internal string ReminderText { get; private set; }

        [JsonProperty("reminderTime")] internal DateTime TriggerTime { get; private set; }

        [JsonProperty("setTime")] internal DateTime SetTime { get; private set; }

        internal Reminder(ulong userId, ulong channelId, string guildId, ulong messageId, int reminderId,
            string reminderText, DateTime triggerTime, DateTime setTime)
        {
            UserId = userId;
            ChannelId = channelId;
            GuildId = guildId;
            MessageId = messageId;
            ReminderId = reminderId;
            ReminderText = reminderText;
            TriggerTime = triggerTime;
            SetTime = setTime;
        }

        private Reminder() { }

        internal long GetTriggerTimeTimestamp()
        {
            return TriggerTime.ToUnixTimeSeconds();
        }

        internal long GetSetTimeTimestamp()
        {
            return SetTime.ToUnixTimeSeconds();
        }

        internal string GetJumpLink()
        {
            return $"https://discord.com/channels/{GuildId}/{ChannelId}/{MessageId}";
        }

        internal DiscordEmbedBuilder CreateEmbed()
        {
            var reminderEmbed = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#7287fd"),
                Title = $"Reminder from <t:{GetSetTimeTimestamp()}:R>",
                Description = ReminderText
            };

            string context;
            if (GuildId == "@me")
            {
                context = "This reminder was set privately, so I can't link back to the message where it was set!" +
                    $" However, [this link]({GetJumpLink()}) should show you messages around the time that you set the reminder.";
            }
            else
            {
                context = GetJumpLink();
            }
            reminderEmbed.AddField("Context", context);

            return reminderEmbed;
        }

        internal static (DateTime? parsedTime, string error) ParseTriggerTime(string triggerTime)
        {
            if (!DateTime.TryParse(triggerTime, out DateTime parsedTime))
            {
                try
                {
                    parsedTime = HumanDateParser.HumanDateParser.Parse(triggerTime);
                }
                catch (ParseException)
                {
                    return (null, $"I couldn't parse \"{triggerTime}\" as a time! Please try again.");
                }
            }

            if (parsedTime <= DateTime.Now)
            {
                return (null, "You can't set a reminder for a time in the past!");
            }

            return (parsedTime, null);
        }

        internal static async Task<(Setup.Types.Reminder reminder, string error)> GetReminderAsync(string reminderId, ulong requestingUserId)
        {
            if (!Setup.Constants.RegularExpressions.ReminderIdPattern.IsMatch(reminderId))
                return (null, "The reminder ID you provided isn't correct! It should look something like this: `1234`." +
                    $" You can see your reminders and their IDs with {"reminder list".AsSlashCommandMention()}.");

            Setup.Types.Reminder reminder;
            try
            {
                reminder = JsonConvert.DeserializeObject<Setup.Types.Reminder>(await Setup.Storage.Redis.HashGetAsync("reminders", reminderId));
            }
            catch (Exception ex) when (ex is ArgumentNullException or JsonReaderException)
            {
                return (null, "I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`." +
                    $" You can see your reminders and their IDs with {"reminder list".AsSlashCommandMention()}.");
            }

            if (reminder.UserId != requestingUserId)
                return (null, "I couldn't find a reminder with that ID! Make sure it's correct. It should look something like this: `1234`." +
                    $" You can see your reminders and their IDs with {"reminder list".AsSlashCommandMention()}.");

            return (reminder, null);
        }

        internal static async Task<List<Setup.Types.Reminder>> GetUserRemindersAsync(ulong userId)
        {
            return (await Setup.Storage.Redis.HashGetAllAsync("reminders"))
                .Select(x => JsonConvert.DeserializeObject<Setup.Types.Reminder>(x.Value)).Where(r => r is not null && r.UserId == userId)
                .OrderBy(x => x.TriggerTime)
                .ToList();
        }

        internal static async Task<int> GenerateUniqueIdAsync()
        {
            Random random = new();
            var reminderId = random.Next(1000, 9999);

            var reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");
            while (reminders.Any(x => x.Name == reminderId))
                reminderId = random.Next(1000, 9999);

            return reminderId;
        }
    }

    internal class ShellCommandResponse
    {
        internal ShellCommandResponse(int exitCode, string output, string error)
        {
            ExitCode = exitCode;
            Output = output;
            Error = error;
        }

        internal ShellCommandResponse(int exitCode, string output)
        {
            ExitCode = exitCode;
            Output = output;
            Error = default;
        }

        private ShellCommandResponse()
        {
            ExitCode = default;
            Output = default;
            Error = default;
        }

        internal int ExitCode { get; private set; }
        internal string Output { get; private set; }
        internal string Error { get; private set; }
    }

    internal static class Apis
    {
        internal static class CatDogApi
        {
            internal class CatDogImage
            {
                [JsonProperty("id")] internal string Id { get; private set; }
                [JsonProperty("url")] internal string Url { get; private set; }
                [JsonProperty("width")] internal int Width { get; private set; }
                [JsonProperty("height")] internal int Height { get; private set; }

                private CatDogImage() { }
            }
        }

        internal static class FactApi
        {
            internal class Fact
            {
                [JsonProperty("id")] internal string Id { get; private set; }
                [JsonProperty("text")] internal string Text { get; private set; }
                [JsonProperty("source")] internal string Source { get; private set; }
                [JsonProperty("source_url")] internal string SourceUrl { get; private set; }
                [JsonProperty("language")] internal string Language { get; private set; }
                [JsonProperty("permalink")] internal string Permalink { get; private set; }

                private Fact() { }
            }
        }
    }

    internal enum GuildEventType
    {
        Join = 0,
        Leave = 1
    }

    internal enum MessageEventType
    {
        Create = 0,
        Update = 1,
        Delete = 2
    }
}

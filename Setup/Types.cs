namespace MechanicalMilkshake.Setup;

internal class Types
{
    internal class DebugInfo
    {
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
            var commitHash = await FileHelpers.ReadFileAsync("CommitHash.txt");
            var commitTimestamp = $"<t:{((DateTimeOffset)Convert.ToDateTime(await FileHelpers.ReadFileAsync("CommitTime.txt"))).ToUnixTimeSeconds()}:F>";
            var commitMessage = await FileHelpers.ReadFileAsync("CommitMessage.txt");
            var remoteUrl = await FileHelpers.ReadFileAsync("RemoteUrl.txt");

            return $"[`{commitHash}`]({remoteUrl}/commit/{commitHash}): {commitMessage}\n{commitTimestamp}";
#endif
        }

        internal DebugInfo(
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

        internal string DotNetVersion { get; }
        internal string OperatingSystem { get; }
        internal string DSharpPlusVersion { get; }
        internal string CommitInformation { get; }
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

                return userKeywords.Select(keyword => new DiscordAutoCompleteChoice(keyword.Keyword, keyword.Id.ToString())).ToList();
            }
        }
    }

    internal class ChoiceProviders
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

    internal class MessageCaching
    {
        internal class CachedMessage
        {
            internal CachedMessage(ulong channelId, ulong messageId, ulong authorId)
            {
                ChannelId = channelId;
                MessageId = messageId;
                AuthorId = authorId;
            }

            internal ulong ChannelId { get; }
            internal ulong MessageId { get; }
            internal ulong AuthorId { get; }
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
        [JsonProperty("keyword")] internal string Keyword { get; set; }

        [JsonProperty("userId")] internal ulong UserId { get; set; }

        [JsonProperty("matchWholeWord")] internal bool MatchWholeWord { get; set; }

        [JsonProperty("ignoreBots")] internal bool IgnoreBots { get; set; }

        [JsonProperty("assumePresence")] internal bool AssumePresence { get; set; }

        [JsonProperty("userIgnoreList")] internal List<ulong> UserIgnoreList { get; set; }

        [JsonProperty("channelIgnoreList")] internal List<ulong> ChannelIgnoreList { get; set; }

        [JsonProperty("guildIgnoreList")] internal List<ulong> GuildIgnoreList { get; set; }

        [JsonProperty("id")] internal ulong Id { get; set; }

        [JsonProperty("guildId")] internal ulong GuildId { get; set; }
    }

    internal class Reminder
    {
        [JsonProperty("userId")] internal ulong UserId { get; set; }

        [JsonProperty("channelId")] internal ulong ChannelId { get; set; }

        [JsonProperty("guildId")] internal string GuildId { get; set; }

        [JsonProperty("messageId")] internal ulong MessageId { get; set; }

        [JsonProperty("reminderId")] internal int ReminderId { get; set; }

        [JsonProperty("reminderText")] internal string ReminderText { get; set; }

        [JsonProperty("reminderTime")] internal DateTime TriggerTime { get; set; }

        [JsonProperty("setTime")] internal DateTime SetTime { get; set; }
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

        internal ShellCommandResponse()
        {
            ExitCode = default;
            Output = default;
            Error = default;
        }

        internal int ExitCode { get; set; }
        internal string Output { get; set; }
        internal string Error { get; set; }
    }

    internal class Apis
    {
        internal class CatDogApi
        {
            internal class CatDogImage
            {
                [JsonProperty("id")] internal string Id { get; private set; }
                [JsonProperty("url")] internal string Url { get; private set; }
                [JsonProperty("width")] internal int Width { get; private set; }
                [JsonProperty("height")] internal int Height { get; private set; }
            }
        }

        internal class FactApi
        {
            internal class Fact
            {
                [JsonProperty("id")] internal string Id { get; private set; }
                [JsonProperty("text")] internal string Text { get; private set; }
                [JsonProperty("source")] internal string Source { get; private set; }
                [JsonProperty("source_url")] internal string SourceUrl { get; private set; }
                [JsonProperty("language")] internal string Language { get; private set; }
                [JsonProperty("permalink")] internal string Permalink { get; private set; }
            }
        }
    }

    internal enum GuildEventType
    {
        Join = 0,
        Leave = 1
    }
}

namespace MechanicalMilkshake;

internal static class ServerSpecificFeatures
{
    internal static class EventChecks
    {
        internal static async Task MessageCreateChecks(MessageCreatedEventArgs e)
        {
            // ignore dms
            if (e.Channel.IsPrivate)
                return;

            #region my server
            if (e.Guild.Id == 799644062973427743)
            {
                #region &caption -> #captions
                if (e.Message.Author.Id == 1031968180974927903 && string.IsNullOrEmpty(e.Message.Content) &&
                    (e.Message.ReferencedMessage is not null && e.Message.ReferencedMessage.Content.Contains("caption")
                    || (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1).ToListAsync())[0].Content.Contains("caption")))
                {
                    await e.Message.ForwardAsync(1048242806486999092);
                }
                #endregion &caption -> #captions
            }
            #endregion my server

            await DoPatchTuesdayMessageCheck(e);
        }

        private static async Task DoPatchTuesdayMessageCheck(MessageCreatedEventArgs e)
        {
            // Ignore messages in wrong channel or by wrong author
            if (e.Channel.Id != 970467567552569365          // reddit mod #bot-spam
                || (e.Author.Id != 1563348673017020497      // Windows11 relay
                    && e.Author.Id != 1563352319830007889)) // Windows10 relay
            {
                return;
            }

            string insiderRedditUrlPattern = @"https:\/\/.*reddit\.com\/r\/Windows1[01]\/comments\/.+\/cumulative_updates[^ ]+";
            var match = Regex.Match(e.Message.Content, insiderRedditUrlPattern);

            // Ignore messages that don't have a URL matching the expected format
            if (!match.Success)
            {
                await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent($"<@455432936339144705> This message was sent by the relay but doesn't match the expected format!")
                    .WithReply(e.Message.Id)
                    .WithAllowedMention(new UserMention(455432936339144705)));
                return;
            }
            
            // Get last 5 messages before current message; if messages are not a Cumulative Updates post, ignore
            var previousMessages = await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 5).ToListAsync();
            foreach (var previousMessage in previousMessages)
            {
                // Ignore messages older than 1 hour, they are probably not what we want
                if (previousMessage.CreationTimestamp < DateTime.Now.AddMinutes(-1))
                    continue;

                // Get URLs from both messages
                var thisUrl = match.Value;
                var previousUrl = Regex.Match(previousMessage.Content, insiderRedditUrlPattern).Value;

                // Figure out which URL is Windows 10...
                string windows10Url;
                if (thisUrl.Contains("windows10", StringComparison.OrdinalIgnoreCase))
                    windows10Url = thisUrl;
                else if (previousUrl.Contains("windows10", StringComparison.OrdinalIgnoreCase))
                    windows10Url = previousUrl;
                else
                    continue; // Skip to the next of the previous messages. This probably shouldn't happen.

                // ...and which is Windows 11
                string windows11Url;
                if (thisUrl.Contains("windows11", StringComparison.OrdinalIgnoreCase))
                    windows11Url = thisUrl;
                else if (previousUrl.Contains("windows11", StringComparison.OrdinalIgnoreCase))
                    windows11Url = previousUrl;
                else
                    continue; // Skip to the next of the previous messages. This probably shouldn't happen.

                // Construct Patch Tuesday announcement
                var msg = $"<@455432936339144705> <@228574821590499329>\n```\nIt's <@&445773142233710594>! Update discussion threads & changelist links are here: {windows10Url} (Windows 10 Extended Security Updates) and {windows11Url} (Windows 11)\n```";

                // Send message
                var targetChannel = await Setup.State.Discord.Client.GetChannelAsync(1251028070488477716);
                await targetChannel.SendMessageAsync(msg);

                // React to relevant messages
                await e.Message.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));
                await Task.Delay(1000); // precaution because reaction ratelimits are stupid
                await previousMessage.CreateReactionAsync(DiscordEmoji.FromUnicode("✅"));

                break;
            }
        }
    }

    internal static class Commands
    {
        internal static class MessageCommands
        {
            // This command has no attributes to restrict where it can be used, because it is only registered to a single server anyway
            [Command("poop")]
            [Description("immaturity is key")]
            [TextAlias("shit", "defecate")]
            [AllowedProcessors(typeof(TextCommandProcessor))]
            public static async Task Poop(CommandContext ctx, [RemainingText] string much = "")
            {
                try
                {
                    DiscordChannel chan;
                    DiscordMessage msg;
#if DEBUG
                    chan = await Setup.State.Discord.Client.GetChannelAsync(893654247709741088);
                    msg = await chan.GetMessageAsync(1282187612844589168);
#else
                    chan = await Setup.State.Discord.Client.GetChannelAsync(892978015309557870);
                    msg = much == "MUCH" ? await chan.GetMessageAsync(1294869494648279071) : await chan.GetMessageAsync(1085253151155830895);
#endif

                    var phrases = msg.Content.Split("\n");

                    var content = phrases[new Random().Next(0, phrases.Length)].Replace("{user}", ctx.Member.DisplayName);

                    await ctx.Channel.SendMessageAsync(content);
                }
                catch (Exception)
                {
                    await ctx.Channel.SendMessageAsync("sorry, i had an accident. please tell milkshake for me.");
                }
            }
        }
    }
}

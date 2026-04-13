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
                if (e.Message.Author.Id == 1031968180974927903 &&
                    (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1).ToListAsync())[0].Content.Contains("caption"))
                {
                    var chan = await Setup.State.Discord.Client.GetChannelAsync(1048242806486999092);
                    if (e.Message.Flags?.HasFlag(DiscordMessageFlags.IsComponentsV2) ?? false)
                    {
                        var mediaGalleryComponent = e.Message.Components[0] as DiscordMediaGalleryComponent;
                        var mediaUrl = mediaGalleryComponent.Items[0].Media.Url;
                        await chan.SendMessageAsync($"{mediaUrl} ({e.Message.JumpLink})");
                    }
                    else if (string.IsNullOrWhiteSpace(e.Message.Content))
                    {
                        await chan.SendMessageAsync($"{e.Message.Attachments[0].Url} ({e.Message.JumpLink})");
                    }
                    else if (e.Message.Content.Contains("http"))
                    {
                        await chan.SendMessageAsync(e.Message.Content);
                    }
                }
                #endregion &caption -> #captions
            }
            #endregion my server

            #region Patch Tuesday announcements
#if DEBUG
            if (e.Guild.Id == 799644062973427743) // my server
            {
                await PatchTuesdayAnnouncementCheck(e, 455432936339144705, 1409289579139305573);
            }
#else
            if (e.Guild.Id == 438781053675634713) // not my server
            {
                await PatchTuesdayAnnouncementCheck(e, 696333378990899301, 1251028070488477716);
            }
#endif
            #endregion Patch Tuesday announcements
        }

        private static async Task PatchTuesdayAnnouncementCheck(MessageCreatedEventArgs e, ulong authorId, ulong channelId)
        {
            // Patch Tuesday automatic message generation

            var insiderRedditUrlPattern = @"https:\/\/.*reddit.com\/r\/Windows[0-9]{1,}.*cumulative_updates.*";

            // Filter to messages by approved author & channel IDs, and that match the pattern
            if (e.Message.Author.Id != authorId || e.Channel.Id != channelId || !Regex.IsMatch(e.Message.Content, insiderRedditUrlPattern))
                return;

            // List of users to ping with message
            var usersToPing = new List<ulong>
            {
                228574821590499329,
                455432936339144705
            };

            // Get message before current message; if authors do not match or message is not a Cumulative Updates post, ignore
            var previousMessage = (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1).ToListAsync())[0];
            if (previousMessage.Author.Id != e.Message.Author.Id || !Regex.IsMatch(previousMessage.Content, insiderRedditUrlPattern))
                return;

            // Get URLs from both messages
            var thisUrl = Regex.Match(e.Message.Content, insiderRedditUrlPattern).Value;
            var previousUrl = Regex.Match(previousMessage.Content, insiderRedditUrlPattern).Value;

            // Figure out which URL is Windows 10 and which is Windows 11
            var windows10Url = thisUrl.Contains("Windows10") ? thisUrl : previousUrl;
            var windows11Url = thisUrl.Contains("Windows11") ? thisUrl : previousUrl;

            // Assemble message
            var msg = "";
            foreach (var user in usersToPing)
            {
                msg += $"<@{user}> ";
            }
            msg += $"```\nIt's <@&445773142233710594>! Update discussion threads & changelist links are here: {windows10Url} (Windows 10 Extended Security Updates) and {windows11Url} (Windows 11)\n```";

            // Send message
            await e.Message.Channel.SendMessageAsync(msg);
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

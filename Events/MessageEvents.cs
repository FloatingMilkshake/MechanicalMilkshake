namespace MechanicalMilkshake.Events;

public partial class MessageEvents
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static Task MessageUpdated(DiscordClient _, MessageUpdatedEventArgs e)
    {
        Task.Run(async () =>
        {
            try
            {
                await KeywordTrackingHelpers.KeywordCheck(e.Message, true);
            }
            catch (Exception ex)
            {
                await ThrowMessageException(ex, e.Message, true);
            }
        });
        return Task.CompletedTask;
    }

    public static Task MessageCreated(DiscordClient client, MessageCreatedEventArgs e)
    {
        Task.Run(async () =>
        {
            try
            {
                if (Program.ConfigJson.UseServerSpecificFeatures) await ServerSpecificFeatures.EventChecks.MessageCreateChecks(e);
                
                await KeywordTrackingHelpers.KeywordCheck(e.Message);

                if (!e.Channel.IsPrivate)
                {
                    // Add message to cache
                    Program.MessageCache.AddMessage(new CachedMessage(e.Message.Channel.Id, e.Message.Id, e.Message.Author.Id));
                    return;
                }

                await HandleDirectMessageAsync(client, e);
            }
            catch (Exception ex)
            {
                Program.MessageCache.AddMessage(new CachedMessage(e.Message.Channel.Id, e.Message.Id, e.Message.Author.Id));
                await ThrowMessageException(ex, e.Message, false);
            }
        });
        return Task.CompletedTask;
    }

    public static Task MessageDeleted(DiscordClient client, MessageDeletedEventArgs e)
    {
        Task.Run(async () =>
        {
            // If message is in cache, remove
            if (Program.MessageCache.TryGetMessage(e.Message.Id, out var cachedMessage) && cachedMessage.MessageId == e.Message.Id)
                Program.MessageCache.RemoveMessage(cachedMessage.MessageId);
        
            // Add most recent message from channel to cache
            var msg = (await e.Channel.GetMessagesAsync(1).ToListAsync())[0];
            Program.MessageCache.AddMessage(new CachedMessage(msg.Channel.Id, msg.Id, msg.Author.Id));
        });
        return Task.CompletedTask;
    }

    public static async Task ThrowMessageException(Exception ex, DiscordMessage message, bool isEdit)
    {
        // Ignore some HTTP errors
        if (ex is HttpRequestException && ex.Message.Contains("Resource temporarily unavailable")) return;
        
        // Handle redis timeout errors differently
        if (await ErrorEvents.DatabaseConnectionErrored(ex)) return;
    
        DiscordEmbedBuilder embed = new()
        {
            Color = DiscordColor.Red,
            Description =
                $"`{ex.GetType()}` occurred when processing [this message]({message.JumpLink}) (message `{message.Id}` in channel `{message.Channel.Id}`).",
            Title = isEdit
                ? "An exception occurred when processing a message update event"
                : "An exception occurred when processing a message create event"
        };
        embed.AddField("Message", $"{ex.Message}");

        Program.Discord.Logger.LogError(Program.BotEventId, "An exception occurred when processing a message {eventType} event!"
            + "\n{exType}: {exMessage}\n{exStackTrace}", isEdit ? "edit" : "create", ex.GetType(), ex.Message, ex.StackTrace);

        await Program.HomeChannel.SendMessageAsync(embed);
    }
    
    private static async Task HandleDirectMessageAsync(DiscordClient client, MessageCreatedEventArgs e)
    {
        // Ignore self
        if (e.Author.IsCurrent)
            return;
        
        if (client.CurrentApplication.Owners.Contains(e.Author))
        {
            // If this is a message from an owner & it is a reply & the message being replied to has an embed whose title contains "DM received from"...
            var reply = e.Message.ReferencedMessage;
            if (reply is not null && reply.Author.IsCurrent
                && reply.Embeds.Any() && reply.Embeds[0].Title is not null
                && reply.Embeds[0].Title.Contains("DM received from"))
            {
                // This is a reply from an owner, forward
                
                // Don't allow both reply and sendto
                if (e.Message.Content.StartsWith("sendto"))
                {
                    await e.Message.RespondAsync(
                        "Did you mean to reply, or to use `sendto`? Please only do one at a time.");
                    return;
                }

                // Get user ID from embed in message being replied to, parse as ulong & fetch user
                var userIdField = e.Message.ReferencedMessage.Embeds[0].Fields.First(f => f.Name == "User ID");
                var userId = Convert.ToUInt64(userIdField.Value.Replace("`", ""));
                var user = await client.GetUserAsync(userId);

                // Get the message ID from the embed in the message being replied to, parse as ulong
                var messageIdField = e.Message.ReferencedMessage.Embeds[0].Fields.First(f => f.Name == "Message ID");
                var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                // Add attachment URLs to content for to-be-forwarded msg, if there are any
                var attachmentUrls = "";
                string messageToSend;
                if (e.Message.Attachments.Count != 0)
                {
                    attachmentUrls = e.Message.Attachments.Aggregate(attachmentUrls,
                        (current, attachment) => current + $"{attachment.Url}\n");

                    messageToSend = $"{e.Message.Content}\n{attachmentUrls}";
                }
                else
                {
                    messageToSend = e.Message.Content;
                }

                // Put together the message to be sent, as a reply to the message being replied to
                var replyBuilder = new DiscordMessageBuilder().WithContent(messageToSend).WithReply(messageId);

                // Send the message
                var msg = await user.SendMessageAsync(replyBuilder);

                // Sent successfully, all done
                var messageBuilder = new DiscordMessageBuilder().WithContent($"Sent! (`{msg.Id}` in `{msg.Channel.Id}`)").WithReply(e.Message.Id);
                await e.Channel.SendMessageAsync(messageBuilder);
            }
            else if (e.Message.Content.Contains("sendto"))
            {
                // Owner using sendto, forward content
                
                var idPattern = IdPattern();
                DiscordChannel targetChannel = default;
                DiscordUser targetUser = default;

                if (idPattern.IsMatch(e.Message.Content))
                {
                    // Message probably contains an ID; try to fetch channel?
                    try
                    {
                        targetChannel = await client.GetChannelAsync(Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString()));
                    }
                    catch
                    {
                        // Not a channel ID. Try to fetch user?
                        try
                        {
                            targetUser = await client.GetUserAsync(Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString()));
                        }
                        catch (Exception ex)
                        {
                            // Not a user ID either. Invalid...
                            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent($"Hmm, that doesn't look like a valid ID! Make sure it's a user or channel ID!" +
                                             $"\n```\n{ex.GetType()}: {ex.Message}\n```")
                                .WithReply(e.Message.Id));
                            return;
                        }
                    }
                }
                else
                {
                    // Message didn't match ID regex; there is no ID in it
                    await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent("Hmm, I couldn't find an ID in your message, so I don't know who to send it to! Please include a user ID or channel ID.")
                        .WithReply(e.Message.Id));
                    return;
                }

                if (targetChannel == default)
                {
                    // Failed to fetch a channel with the ID provided. It must be a user ID.
                    // Try to create a DM channel with the user
                    targetChannel = await targetUser.CreateDmChannelAsync();
                }

                var contentPattern = ContentPattern(); // TODO: redo this regexp?
                string content;
                try
                {
                    // Get content from sendto msg
                    content = contentPattern.Matches(e.Message.Content)[0].Groups[1].Value;
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Failed to get content
                    await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent("I couldn't parse the content in your message! Make sure you provided content after the user or channel ID, and be sure you used the right order (`sendto <id> <content>`)!")
                        .WithReply(e.Message.Id));
                    return;
                }

                // Add attachment URLs to content for to-be-forwarded msg
                if (e.Message.Attachments.Any())
                    content = e.Message.Attachments.Aggregate(content,
                        (current, attachment) => current + $"\n{attachment.Url}");

                DiscordMessage message;
                try
                {
                    // Try sending DM
                    message = await targetChannel.SendMessageAsync(content);
                }
                catch (Exception ex)
                {
                    // Failed to DM
                    await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"Hmm, I couldn't send that message!\n```\n{ex.GetType()}: {ex.Message}\n```")
                        .WithReply(e.Message.Id));
                    return;
                }

                // Sent successfully, all done
                await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                    .WithContent($"Sent! (`{message.Id}` in `{message.Channel.Id}`)")
                    .WithReply(e.Message.Id));
            }
            // else: DM from owner, but not a reply or sendto; ignore
        }
        else
        {
            // Message from non-owner, forward
            
            // Wrap in try/catch so the error and message can be logged
            try
            {
                // TODO: warning: if there are no owners (if the bot is owned by a team), this will do nothing! Also see issue #33
                foreach (var owner in client.CurrentApplication.Owners)
                {
                    // Construct embed to send to owner
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = DiscordColor.Yellow,
                        Title = $"DM received from {UserInfoHelpers.GetFullUsername(e.Author)}!",
                        Description = $"{e.Message.Content}",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("User ID", $"`{e.Author.Id}`", true);
                    embed.AddField("User Mention", $"{e.Author.Mention}", true);
                    embed.AddField("User Avatar URL", $"[Link]({e.Author.AvatarUrl})", true);
                    embed.AddField("Channel ID", $"`{e.Channel.Id}`", true);
                    embed.AddField("Message ID", $"`{e.Message.Id}`", true);
                    var attachmentUrls = "";
                    if (e.Message.Attachments.Count != 0)
                    {
                        attachmentUrls = e.Message.Attachments.Aggregate(attachmentUrls,
                            (current, attachment) => current + $"{attachment.Url}\n");

                        embed.AddField("Attachments", attachmentUrls, true);
                    }

                    DiscordMessageBuilder messageBuilder = new();

                    var isReply = "No";
                    if (e.Message.ReferencedMessage is not null)
                    {
                        // If the user's message is a reply, add a button to show the owner reply info
                        isReply = "Yes";
                        DiscordButtonComponent button = new(DiscordButtonStyle.Primary,
                            "view-dm-reply-info", "View Reply Info");
                        messageBuilder = messageBuilder.AddActionRowComponent(button);
                    }

                    embed.AddField("Is Reply", isReply);

                    // If there are messages before this msg being forwarded, add a button to show the owner some context
                    var messages =
                        await e.Channel.GetMessagesBeforeAsync(e.Message.Id).ToListAsync();
                    var contextExists = false;
                    foreach (var msg in messages)
                        if (msg.Content is not null)
                            contextExists = true;
                    if (contextExists)
                    {
                        DiscordButtonComponent button = new(DiscordButtonStyle.Primary, "view-dm-context",
                            "View Context");
                        messageBuilder.AddActionRowComponent(button);
                    }

                    messageBuilder = messageBuilder.AddEmbed(embed.Build());

                    // Send the message to the owner
                    await owner.SendMessageAsync(messageBuilder);
                    return;
                }
            }
            catch (Exception ex)
            {
                // Some part of the above process failed; the DM was not forwarded. Log to console & home channel if possible
                Program.Discord.Logger.LogError(Program.BotEventId,
                    "A DM was received, but could not be forwarded!\nMessage Content: {MessageContent}\nException Details: {ex.GetType()}: {ExMessage}\n{exStackTrace}",
                    e.Message.Content, ex.GetType(), ex.Message, ex.StackTrace);
                
                try
                {
                    await Program.HomeChannel.SendMessageAsync(new DiscordEmbedBuilder()
                    {
                        Title = "An exception occurred while forwarding a DM",
                        Color = DiscordColor.Red,
                        Description = $"An exception was thrown when trying to forward a DM from {e.Message.Author.Mention} (`{e.Message.Author.Id}`) to a bot owner!"
                    }
                    .AddField("Message Content", string.IsNullOrWhiteSpace(e.Message.Content) ? "No content" : e.Message.Content)
                    .AddField("Exception Details", $"```\n{ex.GetType()}: {ex.Message}\n{ex.StackTrace}\n```"));
                }
                catch (Exception ex2)
                {
                    Program.Discord.Logger.LogError(Program.BotEventId,
                        "An exception occurred while forwarding a DM, and I was unable to properly log it here!\n{ex2Type}: {ex2Message}\n{ex2StackTrace}",
                        ex2.GetType(), ex2.Message, ex2.StackTrace);
                    await Program.HomeChannel.SendMessageAsync("An exception occurred while forwarding a DM, and I was unable to properly log it here! Please check the console for details.");
                    // If even this throws an exception, I don't care; it's probably a permission error, and all I care enough to do at that point is to log to console anyway
                }
            }
        }
    }
    
    [GeneratedRegex("[0-9]{5,}")]
    private static partial Regex IdPattern();
    [GeneratedRegex(".*?[0-9]+((?:.|\n)*)")]
    private static partial Regex ContentPattern();
}
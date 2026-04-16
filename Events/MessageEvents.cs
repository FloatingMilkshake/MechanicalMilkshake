namespace MechanicalMilkshake.Events;

internal class MessageEvents
{
    internal static async Task HandleMessageUpdatedEventAsync(DiscordClient _, MessageUpdatedEventArgs e)
    {
        try
        {
            await e.Message.CheckForTrackedKeywordsAsync(true);
        }
        catch (Exception ex)
        {
            await LogMessageEventExceptionAsync(ex, e.Message, Setup.Types.MessageEventType.Update);
        }
    }

    internal static async Task HandleMessageCreatedEventAsync(DiscordClient client, MessageCreatedEventArgs e)
    {
        try
        {
            if (Setup.Configuration.ConfigJson.UseServerSpecificFeatures)
                await ServerSpecificFeatures.EventChecks.MessageCreateChecks(e);

            await e.Message.CheckForTrackedKeywordsAsync();

            if (e.Channel.IsPrivate)
            {
                await HandleDirectMessageAsync(client, e);
            }
            else
            {
                // Add message to cache
                Setup.State.Caches.MessageCache.AddMessage(new Setup.Types.MessageCaching.CachedMessage(e.Message.Channel.Id, e.Message.Id, e.Message.Author.Id));
            }
        }
        catch (Exception ex)
        {
            Setup.State.Caches.MessageCache.AddMessage(new Setup.Types.MessageCaching.CachedMessage(e.Message.Channel.Id, e.Message.Id, e.Message.Author.Id));
            await LogMessageEventExceptionAsync(ex, e.Message, Setup.Types.MessageEventType.Create);
        }
    }

    internal static async Task HandleMessageDeletedEventAsync(DiscordClient _, MessageDeletedEventArgs e)
    {
        try
        {
            // If message is in cache, remove and add most recent message from channel to cache
            if (Setup.State.Caches.MessageCache.TryGetMessage(e.Message.Id, out var cachedMessage) && cachedMessage.MessageId == e.Message.Id)
            {
                Setup.State.Caches.MessageCache.RemoveMessage(cachedMessage.MessageId);

                var msg = (await e.Channel.GetMessagesAsync(1).ToListAsync()).FirstOrDefault();
                if (msg is not null)
                    Setup.State.Caches.MessageCache.AddMessage(new Setup.Types.MessageCaching.CachedMessage(msg.Channel.Id, msg.Id, msg.Author.Id));
            }
        }
        catch (Exception ex)
        {
            await LogMessageEventExceptionAsync(ex, e.Message, Setup.Types.MessageEventType.Delete);
        }
    }

    private static async Task LogMessageEventExceptionAsync(Exception ex, DiscordMessage message, Setup.Types.MessageEventType eventType)
    {
        DiscordEmbedBuilder embed = new()
        {
            Color = DiscordColor.Red,
            Description =
                $"`{ex.GetType()}` occurred when processing [this message]({message.JumpLink}) (message `{message.Id}` in channel `{message.Channel.Id}`).",
            Title = $"An exception occurred when processing a message {eventType.ToString().ToLower()} event"
        };
        embed.AddField("Message", $"{ex.Message}");

        await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(embed);

        Setup.State.Discord.Client.Logger.LogError(ex, "An exception occurred when processing a message {eventType} event!", eventType.ToString().ToLower());
    }

    private static async Task HandleDirectMessageAsync(DiscordClient client, MessageCreatedEventArgs e)
    {
        // Ignore self
        if (e.Author.IsCurrent)
            return;

        if (client.CurrentApplication.Owners.Any(o => o.Id == e.Message.Author.Id))
        {
            await HandleDirectMessageFromBotOwnerAsync(client, e);
            return;
        }

        // If we receive a DM from someone who isn't a bot owner, forward it to owners

        // Wrap in try/catch so the error and message can be logged
        try
        {
            foreach (var owner in client.CurrentApplication.Owners)
            {
                // Construct embed to send to owner
                DiscordEmbedBuilder embed = new()
                {
                    Color = DiscordColor.Yellow,
                    Title = $"DM received from {e.Author.GetFullUsername()}!",
                    Description = $"{e.Message.Content}",
                    Timestamp = DateTime.UtcNow
                };
                embed.AddField("User ID", $"`{e.Author.Id}`", true);
                embed.AddField("User Mention", $"{e.Author.Mention}", true);
                embed.AddField("User Avatar URL", $"[Link]({e.Author.AvatarUrl})", true);
                embed.AddField("Channel ID", $"`{e.Channel.Id}`", true);
                embed.AddField("Message ID", $"`{e.Message.Id}`", true);
                if (e.Message.Attachments.Count != 0)
                    embed.AddField("Attachments", string.Join("\n", e.Message.Attachments.Select(a => a.Url)), true);

                List<DiscordButtonComponent> buttons = [];

                var messages = await e.Channel.GetMessagesBeforeAsync(e.Message.Id).ToListAsync();
                if (messages.Any(m => m.Content is not null))
                    buttons.Add(new(DiscordButtonStyle.Primary, "button-callback-owner-dm-view-context", "View Context"));

                if (e.Message.ReferencedMessage is not null)
                    buttons.Add(new(DiscordButtonStyle.Primary, "button-callback-owner-dm-view-reply-info", "View Reply Info"));

                var messageBuilder = new DiscordMessageBuilder().AddEmbed(embed.Build());
                if (buttons.Count > 0)
                    messageBuilder.AddActionRowComponent(buttons);

                // Send the message to the owner
                await owner.SendMessageAsync(messageBuilder);
            }
        }
        catch (Exception ex)
        {
            // Some part of the above process failed; the DM was not forwarded. Log to console & home channel if possible
            Setup.State.Discord.Client.Logger.LogError(ex, "A DM was received, but could not be forwarded!\nMessage Content: {MessageContent}",
                e.Message.Content);

            try
            {
                await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(new DiscordEmbedBuilder()
                {
                    Title = "An exception occurred while forwarding a DM",
                    Color = DiscordColor.Red,
                    Description = $"An exception was thrown when trying to forward a DM from {e.Message.Author.Mention} (`{e.Message.Author.Id}`) to a bot owner!"
                }
                .AddField("Message Content", string.IsNullOrWhiteSpace(e.Message.Content) ? "No content" : e.Message.Content)
                .AddField("Exception", $"`{ex.GetType()}: {ex.Message}`"));
            }
            catch (Exception ex2)
            {
                Setup.State.Discord.Client.Logger.LogError(ex2, "An exception occurred while forwarding a DM, and I was unable to properly log it!");
                await Setup.Configuration.Discord.Channels.Home.SendMessageAsync("An exception occurred while forwarding a DM, and I was unable to properly log it here! Please check the console for details.");
            }
        }
    }

    private static async Task HandleDirectMessageFromBotOwnerAsync(DiscordClient client, MessageCreatedEventArgs e)
    {
        var reply = e.Message.ReferencedMessage;
        if (reply is not null && reply.Author.IsCurrent
            && reply.Embeds.Any(e => e.Title is not null && e.Title.Contains("DM received from")))
        {
            await HandleDirectMessageReplyFromBotOwnerAsync(client, e);
        }
        else if (e.Message.Content.Contains("sendto"))
        {
            await SendToAsync(client, e);
        }
    }

    private static async Task HandleDirectMessageReplyFromBotOwnerAsync(DiscordClient client, MessageCreatedEventArgs e)
    {
        // This is a reply from an owner. Forward back to the person who sent the message being replied to

        // Don't allow both reply and sendto
        if (e.Message.Content.StartsWith("sendto"))
        {
            await e.Message.RespondAsync("Did you mean to reply, or to use `sendto`? You can't use both!");
            return;
        }

        // Get user ID from embed in message being replied to, parse as ulong & fetch user
        var userIdField = e.Message.ReferencedMessage.Embeds[0].Fields.First(f => f.Name == "User ID");
        var user = await client.GetUserAsync(Convert.ToUInt64(userIdField.Value.Replace("`", "")));

        // Get the message ID from the embed in the message being replied to, parse as ulong
        var messageIdField = e.Message.ReferencedMessage.Embeds[0].Fields.First(f => f.Name == "Message ID");
        var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

        // Add attachment URLs to content for to-be-forwarded msg, if there are any
        var attachmentUrls = "";
        if (e.Message.Attachments.Count != 0)
            attachmentUrls = string.Join("\n", e.Message.Attachments.Select(a => a.Url));

        var msg = await user.SendMessageAsync(new DiscordMessageBuilder()
            .WithContent($"{e.Message.Content}\n{attachmentUrls}".Trim())
            .WithReply(messageId));

        await e.Channel.SendMessageAsync(new DiscordMessageBuilder().WithContent($"Sent! (`{msg.Id}` in `{msg.Channel.Id}`)").WithReply(e.Message.Id));
    }

    private static async Task SendToAsync(DiscordClient client, MessageCreatedEventArgs e)
    {
        // Owner using sendto, forward content

        DiscordChannel targetChannel = default;
        DiscordUser targetUser = default;

        var idMatch = Setup.Constants.RegularExpressions.DiscordIdPattern.Match(e.Message.Content);
        if (idMatch.Success)
        {
            try
            {
                targetChannel = await client.GetChannelAsync(Convert.ToUInt64(idMatch.Value));
            }
            catch (NotFoundException)
            {
                try
                {
                    targetUser = await client.GetUserAsync(Convert.ToUInt64(idMatch.Value));
                    targetChannel = await targetUser.CreateDmChannelAsync();
                }
                catch (FormatException)
                {
                    await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"Hmm, that doesn't look like a valid ID! Make sure it's a user or channel ID.")
                        .WithReply(e.Message.Id));
                    return;
                }
                catch (Exception ex) when (ex is UnauthorizedException or NotFoundException)
                {
                    await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"I can't DM that user!" +
                                     $"\n```\n{ex.GetType()}: {ex.Message}\n```")
                        .WithReply(e.Message.Id));
                    return;
                }
                catch (Exception ex)
                {
                    await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"Something went wrong." +
                                     $"\n```\n{ex.GetType()}: {ex.Message}\n```")
                        .WithReply(e.Message.Id));
                    return;
                }
            }
        }
        else
        {
            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent("Hmm, I couldn't find an ID in your message, so I don't know who to send it to! Please include a user ID or channel ID.")
                .WithReply(e.Message.Id));
            return;
        }

        if (!targetChannel.IsPrivate)
        {
            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                .WithContent("Sorry, you can't send messages into non-DM channels here!")
                .WithReply(e.Message.Id));
            return;
        }

        var content = e.Message.Content.Split(idMatch.Value).Last().Trim();

        if (e.Message.Attachments.Any())
            content = string.Join("\n", e.Message.Attachments.Select(a => a.Url));

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
                .WithContent($"Failed to send DM:\n```\n{ex.GetType()}: {ex.Message}\n```")
                .WithReply(e.Message.Id));
            return;
        }

        await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
            .WithContent($"Sent! (`{message.Id}` in `{message.Channel.Id}`)")
            .WithReply(e.Message.Id));
    }
}

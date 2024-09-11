namespace MechanicalMilkshake.Events;

public partial class MessageEvents
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static Task MessageUpdated(DiscordClient _, MessageUpdateEventArgs e)
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

    public static Task MessageCreated(DiscordClient client, MessageCreateEventArgs e)
    {
        Task.Run(async () =>
        {
            try
            {
                if (Program.ConfigJson.Base.UseServerSpecificFeatures) await ServerSpecificFeatures.Checks.MessageCreateChecks(e);
                
                await KeywordTrackingHelpers.KeywordCheck(e.Message);

                if (!e.Channel.IsPrivate)
                {
                    // Add message to cache
                    Program.MessageCache.AddMessage(new CachedMessage(e.Message.Channel.Id, e.Message.Id, e.Message.Author.Id));
                    return;
                }

                if (e.Author.IsCurrent)
                    return;

                if (client.CurrentApplication.Owners.Contains(e.Author) && e.Message.ReferencedMessage is null)
                {
                    if (!e.Message.Content.StartsWith("sendto"))
                        return;

                    if (e.Message.ReferencedMessage is not null)
                        if (e.Message.ReferencedMessage.Author != client.CurrentUser ||
                        e.Message.ReferencedMessage.Embeds.Count < 1 ||
                        !e.Message.ReferencedMessage.Embeds[0].Title.Contains("DM received"))
                            return;

                    var idPattern = IdPattern();
                    DiscordChannel targetChannel = default;
                    DiscordUser targetUser = default;

                    if (idPattern.IsMatch(e.Message.Content))
                    {
                        try
                        {
                            targetChannel =
                                await client.GetChannelAsync(
                                    Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString()));
                        }
                        catch
                        {
                            try
                            {
                                targetUser =
                                    await client.GetUserAsync(
                                        Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString()));
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                    .WithContent(
                                        $"Hmm, that doesn't look like a valid ID! Make sure it's a user or channel ID!\n```\n{ex.GetType()}: {ex.Message}\n```")
                                    .WithReply(e.Message.Id));
                                return;
                            }
                        }
                    }
                    else
                    {
                        await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent(
                                "Hmm, I couldn't find an ID in your message, so I don't know who to send it to! Please include a user ID or channel ID.")
                            .WithReply(e.Message.Id));
                        return;
                    }

                    if (targetChannel == default)
                    {
                        DiscordGuild mutualServer = default;
                        foreach (var cachedGuild in client.Guilds.Values)
                        {
                            if (!cachedGuild.Members.Values.Contains(targetUser)) continue;
                            mutualServer = await client.GetGuildAsync(cachedGuild.Id);
                            break;
                        }

                        DiscordMember targetMember;
                        try
                        {
                            targetMember = await mutualServer!.GetMemberAsync(targetUser!.Id);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent(
                                    $"I tried to DM that user, but I don't have any mutual servers with them so Discord wouldn't let me send it. Sorry!\n```\n{ex.GetType()}: {ex.Message}\n```")
                                .WithReply(e.Message.Id));
                            return;
                        }

                        targetChannel = await targetMember.CreateDmChannelAsync();
                    }

                    var contentPattern = ContentPattern();
                    string content;
                    try
                    {
                        content = contentPattern.Matches(e.Message.Content)[0].Groups[1].Value;
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent(
                                "I couldn't parse the content in your message! Make sure you provided content after the user or channel ID, and be sure you used the right order (`sendto <id> <content>`)!")
                            .WithReply(e.Message.Id));
                        return;
                    }

                    if (e.Message.Attachments.Any())
                        content = e.Message.Attachments.Aggregate(content,
                            (current, attachment) => current + $"\n{attachment.Url}");

                    DiscordMessage message;
                    try
                    {
                        message = await targetChannel.SendMessageAsync(content);
                    }
                    catch (Exception ex)
                    {
                        await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent(
                                $"Hmm, I couldn't send that message!\n```\n{ex.GetType()}: {ex.Message}\n```")
                            .WithReply(e.Message.Id));
                        return;
                    }

                    await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                        .WithContent($"Sent! (`{message.Id}` in `{message.Channel.Id}`)")
                        .WithReply(e.Message.Id));

                    return;
                }

                if (client.CurrentApplication.Owners.Contains(e.Author) && e.Message.ReferencedMessage is not null &&
                    e.Message.ReferencedMessage.Author.IsCurrent && e.Message.ReferencedMessage.Embeds.Count != 0 &&
                    e.Message.ReferencedMessage.Embeds[0].Title.Contains("DM received from"))
                {
                    // If these conditions are true, a bot owner has replied to a forwarded message. Now we need to forward that reply.

                    if (e.Message.Content.Contains("sendto"))
                    {
                        await e.Message.RespondAsync(
                            "Did you mean to reply, or to use `sendto`? Please only do one at a time.");
                        return;
                    }

                    var userIdField = e.Message.ReferencedMessage.Embeds[0].Fields.First(f => f.Name == "User ID");
                    var userId = Convert.ToUInt64(userIdField.Value.Replace("`", ""));

                    var mutualServersField = e.Message.ReferencedMessage.Embeds[0].Fields
                        .First(f => f.Name is "Cached Mutual Servers" or "Mutual Servers");

                    var mutualIdPattern = MutualServerIdPattern();
                    var firstMutualId = Convert.ToUInt64(mutualIdPattern.Match(mutualServersField.Value).Groups[1].Value);

                    var mutualServer = await client.GetGuildAsync(firstMutualId);
                    var member = await mutualServer.GetMemberAsync(userId);

                    var messageIdField =
                        e.Message.ReferencedMessage.Embeds[0].Fields.First(f => f.Name == "Message ID");
                    var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

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

                    var replyBuilder =
                        new DiscordMessageBuilder().WithContent(messageToSend).WithReply(messageId);

                    var reply = await member.SendMessageAsync(replyBuilder);

                    var messageBuilder = new DiscordMessageBuilder()
                        .WithContent($"Sent! (`{reply.Id}` in `{reply.Channel.Id}`)").WithReply(e.Message.Id);
                    await e.Channel.SendMessageAsync(messageBuilder);
                }
                else
                {
                    try
                    {
                        List<DiscordGuild> mutualServers = new();
                        
                        foreach (var owner in client.CurrentApplication.Owners)
                        foreach (var guild in client.Guilds.Values)
                        {
                            if (!guild.Members.Values.Contains(owner)) continue;
                            var ownerMember = await guild.GetMemberAsync(owner.Id);
                            
                            mutualServers.Add(guild);

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

                            var mutualServersResponseList = "";
                            foreach (var server in mutualServers)
                            {
                                mutualServersResponseList += $"- `{server}`\n";
                            }

                            DiscordMessageBuilder messageBuilder = new();

                            var isReply = "No";
                            if (e.Message.ReferencedMessage is not null)
                            {
                                isReply = "Yes";
                                DiscordButtonComponent button = new(ButtonStyle.Primary,
                                    "view-dm-reply-info", "View Reply Info");
                                messageBuilder = messageBuilder.AddComponents(button);
                            }

                            embed.AddField("Is Reply", isReply);

                            var messages =
                                await e.Channel.GetMessagesBeforeAsync(e.Message.Id);
                            var contextExists = false;
                            foreach (var msg in messages)
                                if (msg.Content is not null)
                                    contextExists = true;

                            if (contextExists)
                            {
                                DiscordButtonComponent button = new(ButtonStyle.Primary, "view-dm-context",
                                    "View Context");
                                messageBuilder.AddComponents(button);
                            }

                            embed.AddField("Cached Mutual Servers", mutualServersResponseList);

                            messageBuilder = messageBuilder.AddEmbed(embed.Build());

                            await ownerMember.SendMessageAsync(messageBuilder);
                            return;
                        }
                    }
                    catch (Exception ex)
                    {
                        Program.Discord.Logger.LogError(Program.BotEventId,
                            "A DM was received, but could not be forwarded!\nException Details: {ex.GetType()}: {ExMessage}\nMessage Content: {MessageContent}",
                            ex.GetType(), ex.Message, e.Message.Content);
                    }
                }
            }
            catch (Exception ex)
            {
                Program.MessageCache.AddMessage(new CachedMessage(e.Message.Channel.Id, e.Message.Id, e.Message.Author.Id));
                await ThrowMessageException(ex, e.Message, false);
            }
        });
        return Task.CompletedTask;
    }

    public static Task MessageDeleted(DiscordClient client, MessageDeleteEventArgs e)
    {
        Task.Run(async () =>
        {
            // If message is in cache, remove
            if (Program.MessageCache.TryGetMessage(e.Message.Id, out var cachedMessage) && cachedMessage.MessageId == e.Message.Id)
                Program.MessageCache.RemoveMessage(cachedMessage.MessageId);
        
            // Add most recent message from channel to cache
            var msg = (await e.Channel.GetMessagesAsync(1))[0];
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

        Console.WriteLine(
            $"{ex.GetType()} occurred when processing a message {(isEdit ? "edit" : "create")} event: {ex.Message}\n{ex.StackTrace}");

        await Program.HomeChannel.SendMessageAsync(embed);
    }
    
    [GeneratedRegex("[0-9]{5,}")]
    private static partial Regex IdPattern();
    [GeneratedRegex(".*[0-9]+ (.*)")]
    private static partial Regex ContentPattern();
    [GeneratedRegex("Guild ([0-9]+);.*$")]
    private static partial Regex MutualServerIdPattern();
}
namespace MechanicalMilkshake.Events;

public class MessageEvents
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
            if (Program.ConfigJson.Base.UsePerServerFeatures) await PerServerFeatures.Checks.MessageCreateChecks(e);

            try
            {
                await KeywordTrackingHelpers.KeywordCheck(e.Message);

                if (!e.Channel.IsPrivate)
                    return;

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

                    Regex usernamePattern = new(".*#[0-9]{4}");
                    Regex idPattern = new("[0-9]{5,}");
                    DiscordChannel targetChannel = default;
                    DiscordUser targetUser = default;
                    DiscordMember targetMember;

                    if (usernamePattern.IsMatch(e.Message.Content))
                    {
                        var usernameMatch = usernamePattern.Match(e.Message.Content).ToString()
                            .Replace("sendto ", "").Trim();

                        DiscordGuild mutualServer = default;
                        foreach (var guild in client.Guilds)
                            if (guild.Value.Members.Any(m =>
                                    $"{UserInfoHelpers.GetFullUsername(m.Value)}" == usernameMatch))
                            {
                                mutualServer = await client.GetGuildAsync(guild.Value.Id);
                                break;
                            }

                        if (mutualServer == default)
                        {
                            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent(
                                    "I tried to DM that user, but I don't have any mutual servers with them so Discord wouldn't let me send it. Sorry!")
                                .WithReply(e.Message.Id));
                            return;
                        }

                        try
                        {
                            targetMember = mutualServer.Members.FirstOrDefault(m =>
                                $"{UserInfoHelpers.GetFullUsername(m.Value)}" == usernameMatch).Value;
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
                    else if (idPattern.IsMatch(e.Message.Content))
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
                                "Hmm, I couldn't find an ID or username in your message, so I don't know who to send it to! Please include a user ID, channel ID, or username.")
                            .WithReply(e.Message.Id));
                        return;
                    }

                    if (targetChannel == default)
                    {
                        DiscordGuild mutualServer = default;
                        foreach (var guildId in client.Guilds)
                        {
                            var server = await client.GetGuildAsync(guildId.Key);

                            if (!server.Members.ContainsKey(targetUser!.Id)) continue;
                            mutualServer = await client.GetGuildAsync(server.Id);
                            break;
                        }

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

                    Regex getContentPattern = new(".*[0-9]+ ");
                    var getContentMatch = getContentPattern.Match(e.Message.Content);
                    var content = e.Message.Content.Replace(getContentMatch.ToString(), "");

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
                        .First(f => f.Name == "Mutual Servers");

                    Regex mutualIdPattern = new(@"[0-9]*;");
                    var firstMutualId = Convert.ToUInt64(mutualIdPattern.Match(mutualServersField.Value)
                        .ToString().Replace(";", "").Replace("`", ""));

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
                        foreach (var owner in client.CurrentApplication.Owners)
                        foreach (var guildPair in client.Guilds)
                        {
                            var guild = await client.GetGuildAsync(guildPair.Key);

                            if (!guild.Members.ContainsKey(owner.Id)) continue;
                            var ownerMember = await guild.GetMemberAsync(owner.Id);

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

                            var mutualServers = "";

                            foreach (var guildId in client.Guilds)
                            {
                                var server = await client.GetGuildAsync(guildId.Key);

                                if (server.Members.ContainsKey(e.Author.Id)) mutualServers += $"- `{server}`\n";
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

                            embed.AddField("Mutual Servers", mutualServers);

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
                await ThrowMessageException(ex, e.Message, false);
            }
        });
        return Task.CompletedTask;
    }

    private static async Task ThrowMessageException(Exception ex, DiscordMessage message, bool isEdit)
    {
        // Ignore some HTTP errors
        if (ex is HttpRequestException && ex.Message.Contains("Resource temporarily unavailable")) return;
    
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
}
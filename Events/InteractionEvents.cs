namespace MechanicalMilkshake.Events;

internal class InteractionEvents
{
    internal static async Task HandleComponentInteractionCreatedEventAsync(DiscordClient client, ComponentInteractionCreatedEventArgs e)
    {
        switch (e.Id)
        {
            case "button-callback-debug-shutdown":
                {
                    if (!Setup.Configuration.ConfigJson.BotCommanders.Contains(e.User.Id.ToString()))
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                            .AsEphemeral(true));
                        return;
                    }

                    if (e.User.Id != e.Message.Interaction.User.Id)
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder().WithContent(
                                "Only the person that used this command can respond to it!").AsEphemeral(true));
                        return;
                    }

                    DiscordButtonComponent shutdownButton = new(DiscordButtonStyle.Danger, "button-callback-debug-shutdown", "Shut Down", true);
                    DiscordButtonComponent cancelButton = new(DiscordButtonStyle.Primary, "button-callback-debug-shutdown-cancel", "Cancel", true);
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("The bot is now shutting down. It will need to be started again manually!")
                            .AddActionRowComponent(shutdownButton, cancelButton));

                    await Task.Delay(1000);
                    await Setup.State.Discord.Client.DisconnectAsync();
                    await Task.Delay(1000);
                    Environment.Exit(0);
                    break;
                }
            case "button-callback-debug-shutdown-cancel":
                {
                    if (!Setup.Configuration.ConfigJson.BotCommanders.Contains(e.User.Id.ToString()))
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                            .AsEphemeral(true));
                        break;
                    }

                    if (e.User.Id != e.Message.Interaction.User.Id)
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder().WithContent(
                                "Only the person that used this command can respond to it!").AsEphemeral(true));
                        return;
                    }

                    DiscordButtonComponent shutdownButton = new(DiscordButtonStyle.Danger, "button-callback-debug-shutdown", "Shut Down", true);
                    DiscordButtonComponent cancelButton = new(DiscordButtonStyle.Primary, "button-callback-debug-shutdown-cancel", "Cancel", true);
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder().WithContent("Shutdown canceled.")
                            .AddActionRowComponent(shutdownButton, cancelButton));
                    break;
                }
            case "button-callback-owner-dm-view-reply-info":
                {
                    var channelIdField =
                        e.Message.Embeds[0].Fields.First(f => f.Name == "Channel ID");
                    var channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

                    var messageIdField =
                        e.Message.Embeds[0].Fields.First(f => f.Name == "Message ID");
                    var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                    var channel = await client.GetChannelAsync(channelId);
                    var message = await channel.GetMessageAsync(messageId);

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = DiscordColor.Blurple,
                        Title = "DM Reply Info",
                        Description = $"{message.ReferencedMessage.Content}"
                    };

                    embed.AddField("Reply ID", $"`{message.ReferencedMessage.Id}`");
                    embed.AddField("Target User ID", $"`{message.ReferencedMessage.Author.Id}`", true);
                    embed.AddField("Target User Mention", $"{message.ReferencedMessage.Author.Mention}", true);

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
                    break;
                }
            case "button-callback-owner-dm-view-context":
                {
                    var channelIdField =
                        e.Message.Embeds[0].Fields.First(f => f.Name == "Channel ID");
                    var channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

                    var messageIdField =
                        e.Message.Embeds[0].Fields.First(f => f.Name == "Message ID");
                    var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                    var channel = await client.GetChannelAsync(channelId);

                    var contextContent = "";
                    ulong contextId = default;
                    DiscordUser contextAuthor = default;
                    DiscordMessage contextMsg = default;
                    foreach (var msg in await channel.GetMessagesBeforeAsync(messageId, 1).ToListAsync())
                    {
                        contextContent = msg.Content;
                        contextId = msg.Id;
                        contextAuthor = msg.Author;
                        contextMsg = msg;
                        // There is only 1 message in the list we're enumerating here, but this makes sure the foreach only runs once to avoid issues just in case.
                        break;
                    }

                    if (string.IsNullOrWhiteSpace(contextContent) && contextMsg?.Embeds is not null)
                        contextContent = "[Embed Content]\n" + contextMsg.Embeds[0].Description;

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = DiscordColor.Blurple,
                        Title = "DM Context Info",
                        Description = $"{contextContent}"
                    };

                    embed.AddField("Context Message ID", $"`{contextId}`");
                    if (contextAuthor is not null)
                    {
                        embed.AddField("Target User ID", $"`{contextAuthor.Id}`", true);
                        embed.AddField("Target User Mention", $"{contextAuthor.Mention}", true);
                    }

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
                    break;
                }
            case "button-callback-avatar-user-context-command-server-avatar":
                {
                    var targetUserId = Convert.ToUInt64(Setup.Constants.RegularExpressions.DiscordIdPattern.Match(e.Message.Content).ToString());
                    var targetUser = await client.GetUserAsync(targetUserId);

                    DiscordMember targetMember;
                    try
                    {
                        targetMember = await e.Guild.GetMemberAsync(targetUserId);
                    }
                    catch
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent(
                                    "Hmm. It doesn't look like that user is in the server, so they don't have a server avatar.")
                                .AsEphemeral(true));
                        return;
                    }

                    if (targetMember.GuildAvatarUrl is null)
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent(
                                    $"{targetUser.Mention} doesn't have a Server Avatar set! Try using the User Avatar button to get their avatar.")
                                .AsEphemeral(true));
                        return;
                    }

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"{targetMember.GuildAvatarUrl.Replace("size=1024", "size=4096")}")
                            .AsEphemeral(true));
                    break;
                }
            case "button-callback-avatar-user-context-command-user-avatar":
                {
                    var targetUserId = Convert.ToUInt64(Setup.Constants.RegularExpressions.DiscordIdPattern.Match(e.Message.Content).ToString());
                    var targetUser = await client.GetUserAsync(targetUserId);

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent($"{targetUser.AvatarUrl.Replace("size=1024", "size=4096")}").AsEphemeral(true));
                    break;
                }
            case "button-callback-clear-confirm":
                {
                    var messagesToClear = Setup.State.Caches.ClearCache;

                    if (!messagesToClear.ContainsKey(e.Message.Id))
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent("These messages have already been deleted!")
                                .AsEphemeral(true));
                        return;
                    }

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AsEphemeral(true));

                    var messages = messagesToClear.GetValueOrDefault(e.Message.Id);

                    try
                    {
                        await e.Channel.DeleteMessagesAsync(messages,
                            $"[Clear by {UserInfoHelpers.GetFullUsername(e.User)}]");
                    }
                    catch (UnauthorizedException)
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                            .WithContent("I don't have permission to delete messages in this channel! Make sure I have the `Manage Messages` permission.")
                            .AsEphemeral(true));
                        return;
                    }
                    // not catching other exceptions because they are handled by generic slash error handler, but this one deserves a clear message

                    messagesToClear.Remove(e.Message.Id);

                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Done!")
                        .AsEphemeral(true));
                    break;
                }
            case "button-callback-track-remove-confirm":
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AsEphemeral(true));

                    var keywordToDelete = e.Message.Embeds[0].Description;

                    var keywords = await Setup.Storage.Redis.HashGetAllAsync("keywords");
                    var keywordFound = false;
                    Setup.Types.TrackedKeyword keyword = default;
                    foreach (var item in keywords)
                    {
                        keyword = JsonConvert.DeserializeObject<Setup.Types.TrackedKeyword>(item.Value);
                        if (keyword.Keyword != keywordToDelete) continue;
                        keywordFound = true;
                        break;
                    }

                    if (keywordFound)
                    {
                        try
                        {
                            await Setup.Storage.Redis.HashDeleteAsync("keywords", keyword.Id);
                        }
                        catch (Exception ex)
                        {
                            await e.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder().WithContent(
                                    $"I ran into an error trying to delete that keyword!\n\"{ex.GetType()}: {ex.Message}\""));
                            return;
                        }

                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                            .WithContent("Keyword removed.").AsEphemeral(true));

                        DiscordMessageBuilder message = new();
                        message.AddEmbed(e.Message.Embeds[0]);
                        message.AddActionRowComponent(new DiscordButtonComponent(DiscordButtonStyle.Danger,
                            "button-callback-track-remove-confirm",
                            "Remove", true));
                    }
                    else
                    {
                        await e.Interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder().WithContent(
                                "It doesn't look like you're tracking that keyword!"));
                    }
                    break;
                }
            case "button-callback-eval-cancel":
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredMessageUpdate);

                    if (!Setup.State.Caches.CancellationTokens.TryGetValue(e.Message.Id, out CancellationTokenSource cancellationTokenSource))
                    {
                        await e.Message.ModifyAsync(new DiscordMessageBuilder().WithContent("Working on it...")
                        .AddActionRowComponent(new DiscordActionRowComponent(
                            [new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-eval-cancel", "Failed to Cancel", true)]
                        )));
                        return;
                    }

                    if (e.User.Id != e.Message.Interaction.User.Id)
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder().WithContent(
                                "Only the person that used this command can cancel it!").AsEphemeral(true));
                        return;
                    }

                    await e.Message.ModifyAsync(new DiscordMessageBuilder().WithContent("Working on it...")
                        .AddActionRowComponent(new DiscordActionRowComponent(
                            [new DiscordButtonComponent(DiscordButtonStyle.Danger, "button-callback-eval-cancel", "Cancelling...", true)]
                        )));
                    cancellationTokenSource.Cancel();

                    break;
                }
            case "selectmenu-callback-reminder-delete":
                {
                    Setup.Types.Reminder reminder;
                    try
                    {
                        reminder =
                            JsonConvert.DeserializeObject<Setup.Types.Reminder>(
                                await Setup.Storage.Redis.HashGetAsync("reminders", e.Values[0]));
                    }
                    catch
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent(
                                    "That reminder was already deleted!")
                                .AsEphemeral(true));
                        return;
                    }

                    try
                    {
                        var reminderChannel = await Setup.State.Discord.Client.GetChannelAsync(reminder.ChannelId);

                        if (reminder.MessageId != default)
                        {
                            var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

                            await reminderMessage.ModifyAsync("This reminder was deleted.");
                        }
                    }
                    catch
                    {
                        // Not important
                    }

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", e.Values[0]);

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent("Reminder deleted successfully.").AsEphemeral(true));
                    break;
                }
            case "selectmenu-callback-reminder-modify":
                {
                    Setup.Types.Reminder reminder;
                    try
                    {
                        reminder = JsonConvert.DeserializeObject<Setup.Types.Reminder>(await Setup.Storage.Redis.HashGetAsync("reminders", e.Values[0]));
                    }
                    catch
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder().WithContent($"Sorry, something went wrong. Please try again or contact a bot owner for help."));
                        return;
                    }

                    if (reminder.UserId != e.Interaction.User.Id)
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                            .WithContent("Only the person who set that reminder can modify it!").AsEphemeral(true));
                        return;
                    }

                    Setup.State.Caches.ReminderModifyCache[e.Interaction.User.Id] = reminder;

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.Modal,
                        new DiscordModalBuilder().WithCustomId("modal-callback-reminder-modify").WithTitle("Modify a Reminder")
                        .AddTextInput(new DiscordTextInputComponent("text-input-callback-reminder-modify-reminder-time", placeholder: reminder.TriggerTime.Humanize(), required: false), "When do you want to be reminded?")
                        .AddTextInput(new DiscordTextInputComponent("text-input-callback-reminder-modify-reminder-text", placeholder: string.IsNullOrWhiteSpace(reminder.ReminderText) ? "[no content]" : reminder.ReminderText, required: false), "What do you want to be reminded about?"));

                    break;
                }
            default:
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent($"Unknown interaction ID `{e.Id}`! Please contact a bot owner for help."));
                    break;
                }
        }
    }

    internal static async Task HandleModalSubmittedEventAsync(DiscordClient _, ModalSubmittedEventArgs e)
    {
        switch (e.Id)
        {
            case "modal-callback-remind-me-about-this":
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AsEphemeral(true));

                    var targetMessage = Setup.State.Caches.ReminderInteractionCache[e.Interaction.User.Id];

                    var timeInput = (e.Values["text-input-callback-remind-me-about-this-time"] as TextInputModalSubmission).Value;

                    DateTime time;
                    try
                    {
                        time = HumanDateParser.HumanDateParser.Parse(timeInput);
                    }
                    catch
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                            .WithContent($"I couldn't parse \"{timeInput}\" as a time! Please try again."));
                        return;
                    }

                    // Create reminder

                    Random random = new();
                    var reminderId = random.Next(1000, 9999);

                    var reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");
                    foreach (var rem in reminders)
                        while (rem.Name == reminderId)
                            reminderId = random.Next(1000, 9999);

                    var reminder = new Setup.Types.Reminder
                    {
                        UserId = e.Interaction.User.Id,
                        ChannelId = e.Interaction.Channel.Id,
                        MessageId = targetMessage.Id,
                        SetTime = DateTime.Now,
                        TriggerTime = time,
                        ReminderId = reminderId,
                        ReminderText = "You set this reminder on a message with the \"Remind Me About This\" command.",
                        GuildId = e.Interaction.Guild is null ? "@me" : e.Interaction.Guild.Id.ToString()
                    };

                    // Save reminder to db
                    await Setup.Storage.Redis.HashSetAsync("reminders", reminderId.ToString(), JsonConvert.SerializeObject(reminder));

                    // Respond
                    var unixTime = ((DateTimeOffset)time).ToUnixTimeSeconds();
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"Alright, I will remind you about [that message](<{targetMessage.JumpLink}>) on <t:{unixTime}:F> (<t:{unixTime}:R>)."));

                    break;
                }
            case "modal-callback-reminder-modify":
                {
                    await e.Interaction.DeferAsync(true);

                    var time = (e.Values["text-input-callback-reminder-modify-reminder-time"] as TextInputModalSubmission).Value;
                    var text = (e.Values["text-input-callback-reminder-modify-reminder-text"] as TextInputModalSubmission).Value;
                    string id = null;
                    if (e.Values.ContainsKey("text-input-callback-reminder-modify-reminder-id"))
                        id = (e.Values["text-input-callback-reminder-modify-reminder-id"] as TextInputModalSubmission).Value;

                    Setup.Types.Reminder reminder;
                    try
                    {
                        if (!Setup.State.Caches.ReminderModifyCache.TryGetValue(e.Interaction.User.Id, out reminder))
                        {
                            if (!Setup.Constants.RegularExpressions.DiscordIdPattern.IsMatch(id))
                            {
                                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                                    .WithContent("The reminder ID you provided isn't correct! It should look something like this: `1234`." +
                                        $" You can see your reminders and their IDs with {CommandHelpers.GetSlashCmdMention("reminder list")}.")
                                    .AsEphemeral(true));
                                return;
                            }

                            reminder = JsonConvert.DeserializeObject<Setup.Types.Reminder>(await Setup.Storage.Redis.HashGetAsync("reminders", id));
                        }
                    }
                    catch (ArgumentNullException)
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                            .WithContent("The reminder ID you provided isn't correct! It should look something like this: `1234`." +
                                        $" You can see your reminders and their IDs with {CommandHelpers.GetSlashCmdMention("reminder list")}.")
                            .AsEphemeral(true));
                        return;
                    }
                    catch
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"Sorry, something went wrong. Please try again or contact a bot owner for help."));
                        return;
                    }

                    if (reminder.UserId != e.Interaction.User.Id)
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                            .WithContent("Only the person who set that reminder can modify it!").AsEphemeral(true));
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(time))
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Reminder unchanged."));
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(text)) reminder.ReminderText = text;

                    if (!string.IsNullOrWhiteSpace(time))
                        try
                        {
                            reminder.TriggerTime = HumanDateParser.HumanDateParser.Parse(time);
                        }
                        catch
                        {
                            // Parse error, either because the user did it wrong or because HumanDateParser is weird

                            await e.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder().WithContent(
                                    $"I couldn't parse \"{time}\" as a time! Please try again."));
                            return;
                        }

                    await Setup.Storage.Redis.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));

                    try
                    {
                        var reminderChannel = await Setup.State.Discord.Client.GetChannelAsync(reminder.ChannelId);

                        if (reminder.MessageId != default)
                        {
                            var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

                            var unixTime = ((DateTimeOffset)reminder.TriggerTime).ToUnixTimeSeconds();

                            if (reminderMessage.Content.Contains("pushed back"))
                                await reminderMessage.ModifyAsync(
                                    $"[Reminder](https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId})" +
                                    $" pushed back to <t:{unixTime}:F> (<t:{unixTime}:R>)!" +
                                    $"\nReminder ID: `{reminder.ReminderId}`");
                            else
                                await reminderMessage.ModifyAsync($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!" +
                                                                  $"\nReminder ID: `{reminder.ReminderId}`");
                        }
                    }
                    catch
                    {
                        // Not important
                    }

                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Reminder modified successfully."));

                    Setup.State.Caches.ReminderModifyCache.Remove(e.Interaction.User.Id);

                    break;
                }
            case "modal-callback-reminder-delete":
                {
                    await e.Interaction.DeferAsync(true);

                    var id = (e.Values["text-input-callback-reminder-delete-reminder-id"] as TextInputModalSubmission).Value;

                    var (reminder, error) = await ReminderHelpers.GetReminderAsync(id, e.Interaction.User.Id);
                    if (reminder is null)
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(error).AsEphemeral(true));
                        return;
                    }

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", id);

                    try
                    {
                        var reminderChannel = await Setup.State.Discord.Client.GetChannelAsync(reminder.ChannelId);

                        if (reminder.MessageId != default)
                        {
                            var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

                            var unixTime = ((DateTimeOffset)reminder.TriggerTime).ToUnixTimeSeconds();

                            await reminderMessage.ModifyAsync("This reminder was deleted.");
                        }
                    }
                    catch
                    {
                        // Not important
                    }

                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Reminder deleted successfully."));

                    break;
                }
            default:
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent($"Unknown interaction ID `{e.Id}`! Please contact a bot owner for help."));
                    break;
                }
        }
    }

    internal static async Task HandleCommandExecutedEventAsync(CommandsExtension _, CommandExecutedEventArgs e)
    {
        await LogInteractionCommandUsageAsync(e.Context);
    }

    private static async Task LogInteractionCommandUsageAsync(CommandContext context)
    {
        try
        {
            // Ignore home server, excluded servers, and authorized users
            if (context.Guild is not null && (context.Guild.Id == Setup.Configuration.Discord.HomeServer.Id || context.Guild.Id == 1342179809618559026 ||
                Setup.Configuration.ConfigJson.SlashCommandLogExcludedGuilds.Contains(context.Guild.Id.ToString())) ||
                Setup.Configuration.ConfigJson.BotCommanders.Contains(context.User.Id.ToString()))
                return;

            // Increment count
            if (await Setup.Storage.Redis.HashExistsAsync("commandCounts", context.Command.FullName))
                await Setup.Storage.Redis.HashIncrementAsync("commandCounts", context.Command.FullName);
            else
                await Setup.Storage.Redis.HashSetAsync("commandCounts", context.Command.FullName, 1);

            // Log to log channel if configured
            if (Setup.Configuration.ConfigJson.SlashCommandLogChannel is not null)
            {
                var description = context.Channel.IsPrivate
                    ? $"{context.User.Username} (`{context.User.Id}`) used {CommandHelpers.GetSlashCmdMention(context.Command.FullName)} in DMs."
                    : $"{context.User.Username} (`{context.User.Id}`) used {CommandHelpers.GetSlashCmdMention(context.Command.FullName)} in `{context.Channel.Name}` (`{context.Channel.Id}`) in \"{context.Guild.Name}\" (`{context.Guild.Id}`).";

                var embed = new DiscordEmbedBuilder()
                    .WithColor(Setup.Constants.BotColor)
                    .WithAuthor(context.User.Username, null, context.User.AvatarUrl)
                    .WithDescription(description)
                    .WithTimestamp(DateTime.Now);

                try
                {
                    await (await context.Client.GetChannelAsync(
                        Convert.ToUInt64(Setup.Configuration.ConfigJson.SlashCommandLogChannel))).SendMessageAsync(embed);
                }
                catch (Exception ex) when (ex is UnauthorizedException or NotFoundException)
                {
                    Setup.State.Discord.Client.Logger.LogError("{User} used {Command} in {Guild} but it could not be logged because the log channel cannot be accessed",
                        context.User.Id, context.Command.FullName, context.Guild.Id);
                }
                catch (FormatException)
                {
                    Setup.State.Discord.Client.Logger.LogError("{User} used {Command} in {Guild} but it could not be logged because the log channel ID is invalid",
                        context.User.Id, context.Command.FullName, context.Guild.Id);
                }
            }
        }
        catch (Exception ex)
        {
            DiscordEmbedBuilder embed = new()
            {
                Title = "An exception was thrown when logging a slash command",
                Description =
                    $"An exception was thrown when {context.User.Mention} used `/{context.Command.FullName}`. Details are below.",
                Color = DiscordColor.Red
            };
            embed.AddField("Exception Details",
                $"```{ex.GetType()}: {ex.Message}:\n{ex.StackTrace}".Truncate(1020) + "\n```");

            await Setup.Configuration.Discord.Channels.Home.SendMessageAsync(embed);
        }

    }
}

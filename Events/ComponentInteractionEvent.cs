namespace MechanicalMilkshake.Events;

public partial class ComponentInteractionEvent
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static async Task ComponentInteractionCreated(DiscordClient s, ComponentInteractionCreatedEventArgs e)
    {
        switch (e.Id)
        {
            case "slash-fail-restart-button"
                when Program.ConfigJson.Base.AuthorizedUsers.Contains(e.User.Id.ToString()):
            {
                DiscordButtonComponent restartButton =
                    new(DiscordButtonStyle.Danger, "slash-fail-restart-button", "Restart", true);

                try
                {
                    var dockerCheckFile = await File.ReadAllTextAsync("/proc/self/cgroup");
                    if (string.IsNullOrWhiteSpace(dockerCheckFile))
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                            new DiscordInteractionResponseBuilder()
                                .WithContent(
                                    "The bot may not be running under Docker and as such cannot be restarted this way! Please restart the bot manually.")
                                .AddComponents(restartButton));
                        return;
                    }
                }
                catch
                {
                    // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                "The bot may not be running under Docker and as such cannot be restarted this way! Please restart the bot manually.")
                            .AddComponents(restartButton));
                    return;
                }

                e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Restarting!")
                        .AddComponents(restartButton));
                Environment.Exit(1);
                break;
            }
            case "slash-fail-restart-button":
            {
                e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                        .AsEphemeral());
                break;
            }
            case "shutdown-button" when Program.ConfigJson.Base.AuthorizedUsers.Contains(e.User.Id.ToString()):
            {
                if (e.User.Id != e.Message.Interaction.User.Id)
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Only the person that used this command can respond to it!").AsEphemeral());
                    return;
                }
                
                DiscordButtonComponent shutdownButton =
                    new(DiscordButtonStyle.Danger, "shutdown-button", "Shut Down", true);
                DiscordButtonComponent cancelButton =
                    new(DiscordButtonStyle.Primary, "shutdown-cancel-button", "Cancel", true);
                e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Warning: The bot is now shutting down. This action cannot be undone.")
                        .AddComponents(shutdownButton, cancelButton));

                await Task.Delay(1000);
                Program.Discord.DisconnectAsync();
                await Task.Delay(1000);
                Environment.Exit(0);
                break;
            }
            case "shutdown-button":
            {
                e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                        .AsEphemeral());
                break;
            }
            case "shutdown-cancel-button" when Program.ConfigJson.Base.AuthorizedUsers.Contains(e.User.Id.ToString()):
            {
                if (e.User.Id != e.Message.Interaction.User.Id)
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(
                            "Only the person that used this command can respond to it!").AsEphemeral());
                    return;
                }
                
                DiscordButtonComponent shutdownButton =
                    new(DiscordButtonStyle.Danger, "shutdown-button", "Shut Down", true);
                DiscordButtonComponent cancelButton =
                    new(DiscordButtonStyle.Primary, "shutdown-cancel-button", "Cancel", true);
                e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Shutdown canceled.")
                        .AddComponents(shutdownButton, cancelButton));
                break;
            }
            case "shutdown-cancel-button":
            {
                e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                        .AsEphemeral());
                break;
            }
            case "view-dm-reply-info":
            {
                var channelIdField =
                    e.Message.Embeds[0].Fields.First(f => f.Name == "Channel ID");
                var channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

                var messageIdField =
                    e.Message.Embeds[0].Fields.First(f => f.Name == "Message ID");
                var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                var channel = await s.GetChannelAsync(channelId);
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
                    new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral());
                break;
            }
            case "view-dm-context":
            {
                var channelIdField =
                    e.Message.Embeds[0].Fields.First(f => f.Name == "Channel ID");
                var channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

                var messageIdField =
                    e.Message.Embeds[0].Fields.First(f => f.Name == "Message ID");
                var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                var channel = await s.GetChannelAsync(channelId);

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
                    new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral());
                break;
            }
            case "server-avatar-ctx-cmd-button":
            {
                var idPattern = IdPattern();
                var targetUserId = Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString());
                var targetUser = await s.GetUserAsync(targetUserId);

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
                            .AsEphemeral());
                    return;
                }

                if (targetMember.GuildAvatarUrl is null)
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                $"{targetUser.Mention} doesn't have a Server Avatar set! Try using the User Avatar button to get their avatar.")
                            .AsEphemeral());
                    return;
                }

                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{targetMember.GuildAvatarUrl.Replace("size=1024", "size=4096")}")
                        .AsEphemeral());
                break;
            }
            case "user-avatar-ctx-cmd-button":
            {
                var idPattern = IdPattern();
                var targetUserId = Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString());
                var targetUser = await s.GetUserAsync(targetUserId);

                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{targetUser.AvatarUrl.Replace("size=1024", "size=4096")}").AsEphemeral());
                break;
            }
            case "code-quick-shortcut":
            {
                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AsEphemeral());

                var ctx = e.Interaction;
                try
                {
                    EventGlobals globals = new(Program.Discord, ctx);

                    var scriptOptions = ScriptOptions.Default;
                    scriptOptions = scriptOptions.WithImports(EvalCommands.EvalImports);
                    scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                        .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                    var script = CSharpScript.Create(e.Message.Content, scriptOptions, typeof(EventGlobals));
                    script.Compile();
                    var result = await script.RunAsync(globals).ConfigureAwait(false);

                    if (result is null)
                    {
                        await ctx.DeleteOriginalResponseAsync();
                    }
                    else
                    {
                        if (result.ReturnValue is null)
                        {
                            await ctx.DeleteOriginalResponseAsync();
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                            {
                                // Isn't null, so it has to be whitespace
                                await ctx.CreateFollowupMessageAsync(
                                    new DiscordFollowupMessageBuilder().WithContent($"\"{result.ReturnValue}\""));
                                return;
                            }

                            await ctx.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder().WithContent(result.ReturnValue.ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(ex.GetType() + ": " + ex.Message).AsEphemeral());
                }

                break;
            }
            case "clear-confirm-callback":
            {
                Task.Run(async () =>
                {
                    var messagesToClear = Clear.MessagesToClear;

                    if (!messagesToClear.ContainsKey(e.Message.Id))
                    {
                        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                            new DiscordInteractionResponseBuilder()
                                .WithContent("These messages have already been deleted!")
                                .AsEphemeral());
                        return;
                    }

                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AsEphemeral());

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
                            .AsEphemeral());
                        return;
                    }
                    // not catching other exceptions because they are handled by generic slash error handler, but this one deserves a clear message

                    messagesToClear.Remove(e.Message.Id);

                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Done!")
                        .AsEphemeral());
                });
                break;
            }
            case "track-remove-confirm-button":
            {
                Task.Run(async () =>
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AsEphemeral());

                    var keywordToDelete = e.Message.Embeds[0].Description;

                    var keywords = await Program.Db.HashGetAllAsync("keywords");
                    var keywordFound = false;
                    TrackedKeyword keyword = default;
                    foreach (var item in keywords)
                    {
                        keyword = JsonConvert.DeserializeObject<TrackedKeyword>(item.Value);
                        if (keyword!.Keyword != keywordToDelete) continue;
                        keywordFound = true;
                        break;
                    }

                    if (keywordFound)
                    {
                        try
                        {
                            await Program.Db.HashDeleteAsync("keywords", keyword.Id);
                        }
                        catch (Exception ex)
                        {
                            await e.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder().WithContent(
                                    $"I ran into an error trying to delete that keyword!\n\"{ex.GetType()}: {ex.Message}\""));
                            return;
                        }

                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                            .WithContent("Keyword removed.").AsEphemeral());
                        
                        DiscordMessageBuilder message = new();
                        message.AddEmbed(e.Message.Embeds[0]);
                        message.AddComponents(new DiscordButtonComponent(DiscordButtonStyle.Danger,
                            "track-remove-confirm-button",
                            "Remove", true));
                    }
                    else
                    {
                        await e.Interaction.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder().WithContent(
                                "It doesn't look like you're tracking that keyword!"));
                    }
                });
                break;
            }
            case "reminder-delete-dropdown":
            {
                Reminder reminder;
                try
                {
                    reminder =
                        JsonConvert.DeserializeObject<Reminder>(
                            await Program.Db.HashGetAsync("reminders", e.Values[0]));
                }
                catch
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                "That reminder was already deleted!")
                            .AsEphemeral());
                    return;
                }

                if (!reminder!.IsPrivate)
                {
                    var reminderChannel = await Program.Discord.GetChannelAsync(reminder.ChannelId);
                    
                    if (reminder.MessageId != default)
                    {
                        var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

                        await reminderMessage.ModifyAsync("This reminder was deleted.");
                    }
                }

                await Program.Db.HashDeleteAsync("reminders", e.Values[0]);

                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("Reminder deleted successfully.").AsEphemeral());
                break;
            }
            case "reminder-show-dropdown":
            {
                Reminder reminder;
                try
                {
                    reminder =
                        JsonConvert.DeserializeObject<Reminder>(
                            await Program.Db.HashGetAsync("reminders", e.Values[0]));
                }
                catch
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                "That reminder doesn't exist! Perhaps it was deleted before you chose it from the list?")
                            .AsEphemeral());
                    return;
                }

                DiscordEmbedBuilder embed = new()
                {
                    Title = $"Reminder `{e.Values[0]}`",
                    Description = reminder!.ReminderText,
                    Color = Program.BotColor
                };

                if (reminder.GuildId != "@me" && reminder.ReminderTime is not null)
                {
                    embed.AddField("Server",
                        $"{(await Program.Discord.GetGuildAsync(Convert.ToUInt64(reminder.GuildId))).Name}");
                    embed.AddField("Channel", $"<#{reminder.ChannelId}>");
                }

                var jumpLink = reminder.IsPrivate
                    ? $"This reminder was set privately, so the message where it was set is unavailable. Here is a link to the surrounding context: https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}/"
                    : $"https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}/";

                embed.AddField("Jump Link", jumpLink);

                var setTime = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();

                long reminderTime = default;
                if (reminder.ReminderTime is not null)
                    reminderTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

                embed.AddField("Set At", $"<t:{setTime}:F> (<t:{setTime}:R>)");

                if (reminder.ReminderTime is not null)
                    embed.AddField("Set For", $"<t:{reminderTime}:F> (<t:{reminderTime}:R>)");
                else
                    embed.WithFooter(
                        "This reminder will not be sent automatically.");

                await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AddEmbed(embed).AsEphemeral());
                break;
            }
            case "right":
            case "rightskip":
            case "left":
            case "leftskip":
            case "stop":
                break; // avoid getting in the way when pagination is used
            default:
                e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"Unknown interaction ID `{e.Id}`! Contact the bot developer for assistance.").AsEphemeral());
                break;
        }
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex IdPattern();
}

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
public class EventGlobals
{
    public EventGlobals(DiscordClient client, DiscordInteraction ctx)
    {
        Context = ctx;
        Client = client;
        Channel = ctx.Channel;
        Guild = ctx.Guild;
        User = ctx.User;
        if (Guild is not null) Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public DiscordClient Client { get; set; }
    public DiscordMessage Message { get; set; }
    public DiscordChannel Channel { get; set; }
    public DiscordGuild Guild { get; set; }
    public DiscordUser User { get; set; }
    public DiscordMember Member { get; set; }
    public DiscordInteraction Context { get; set; }
}
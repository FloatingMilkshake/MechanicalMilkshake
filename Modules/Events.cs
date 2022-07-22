namespace MechanicalMilkshake.Modules
{
    public class Events
    {
        public class Errors
        {
            public static async Task SlashCommandErrored(SlashCommandsExtension scmds, SlashCommandErrorEventArgs e)
            {
                if (e.Exception is SlashExecutionChecksFailedException exception)
                {
                    if (exception.FailedChecks.OfType<SlashRequireGuildAttribute>().Any())
                    {
                        await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("This command cannot be used in DMs. Please use it in a server. Contact the bot owner if you need help or think I messed up.").AsEphemeral(true));
                        return;
                    }
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up.").AsEphemeral(true));
                    return;
                }

                List<Exception> exs = new();
                if (e.Exception is AggregateException ae)
                    exs.AddRange(ae.InnerExceptions);
                else
                    exs.Add(e.Exception);

                foreach (Exception ex in exs)
                {
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "An exception occurred when executing a slash command",
                        Description = $"`{ex.GetType()}` occurred when executing `{e.Context.CommandName}`.",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("Message", ex.Message);
                    embed.AddField("Debug Info", $"If you'd like to contact the bot owner about this, include this debug info:\n```{ex}\n```");

                    // I don't know how to tell whether the command response was deferred or not, so we're going to try both an interaction response and follow-up so that the interaction doesn't time-out.
                    try
                    {
                        await e.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
                    }
                    catch
                    {
                        // Sometimes a follow-up also doesn't work - if that's the case, we'll forward the error to the bot's home channel.
                        try
                        {
                            await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
                        }
                        catch
                        {
                            embed.Description = $"`{ex.GetType()}` occurred when {e.Context.User.Mention} used `{e.Context.CommandName}`.";
                            await Program.homeChannel.SendMessageAsync(embed);
                        }
                    }
                }
            }

            // Most of the code for this exception handler is from Erisa's Cliptok: https://github.com/Erisa/Cliptok/blob/aabf8aa/Program.cs#L488-L527
            public static async Task CommandsNextService_CommandErrored(CommandsNextExtension cnext, CommandErrorEventArgs e)
            {
                if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                    return;

                List<Exception> exs = new();
                if (e.Exception is AggregateException ae)
                    exs.AddRange(ae.InnerExceptions);
                else
                    exs.Add(e.Exception);

                foreach (Exception ex in exs)
                {
                    if (ex is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                        return;

                    if (ex is ChecksFailedException)
                    {
                        await e.Context.RespondAsync("Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up.");
                        return;
                    }

                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#FF0000"),
                        Title = "An exception occurred when executing a command",
                        Description = $"`{ex.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                        Timestamp = DateTime.UtcNow
                    };
                    embed.AddField("Message", ex.Message);
                    embed.AddField("Debug Info", $"If you'd like to contact the bot owner about this, include this debug info:\n```{ex}\n```");

                    // System.ArgumentException
                    if (ex.GetType().ToString() == "System.ArgumentException")
                    {
                        embed.AddField("What's that mean?", "This usually means that you used the command incorrectly.\n" +
                            $"Please run `help {e.Command.QualifiedName}` for information about what you need to provide for the `{e.Command.QualifiedName}` command.");
                    }

                    // Check if bot has perms to send error response and send if so
                    if (e.Context.Channel.PermissionsFor(await e.Context.Guild.GetMemberAsync(e.Context.Client.CurrentUser.Id)).HasPermission(Permissions.SendMessages))
                    {
                        await e.Context.RespondAsync(embed: embed.Build()).ConfigureAwait(false);
                    }
                }
            }
        }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        public static async Task OnReady(DiscordClient client, ReadyEventArgs e)
        {
            Task.Run(async () =>
            {
                Program.connectTime = DateTime.Now;

                await Program.homeChannel.SendMessageAsync($"Connected!\n{Helpers.GetDebugInfo()}");
            });
        }

        public static async Task ComponentInteractionCreated(DiscordClient s, ComponentInteractionCreateEventArgs e)
        {
            if (e.Id == "slash-fail-restart-button")
            {
                if (Program.configjson.AuthorizedUsers.Contains(e.User.Id.ToString()))
                {
                    DiscordButtonComponent restartButton = new(ButtonStyle.Danger, "slash-fail-restart-button", "Restart", true);

                    try
                    {
                        string dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                        if (string.IsNullOrWhiteSpace(dockerCheckFile))
                        {
                            await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("The bot may not be running under Docker and as such cannot be restarted this way! Please restart the bot manually.").AddComponents(restartButton));
                            return;
                        }
                    }
                    catch
                    {
                        // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("The bot may not be running under Docker and as such cannot be restarted this way! Please restart the bot manually.").AddComponents(restartButton));
                        return;
                    }

                    e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Restarting!").AddComponents(restartButton));
                    Environment.Exit(1);
                }
                else
                {
                    e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{e.User.Mention}, you are not authorized to perform this action!").AsEphemeral(true));
                }
            }
            if (e.Id == "shutdown-button")
            {
                if (Program.configjson.AuthorizedUsers.Contains(e.User.Id.ToString()))
                {
                    DiscordButtonComponent shutdownButton = new(ButtonStyle.Danger, "shutdown-button", "Shut Down", true);
                    DiscordButtonComponent cancelButton = new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel", true);
                    e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("**Warning: The bot is now shutting down. This action is permanent.**").AddComponents(shutdownButton, cancelButton));

                    Program.discord.DisconnectAsync();
                    Environment.Exit(0);
                }
                else
                {
                    e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{e.User.Mention}, you are not authorized to perform this action!").AsEphemeral(true));
                }
            }
            if (e.Id == "shutdown-cancel-button")
            {
                if (Program.configjson.AuthorizedUsers.Contains(e.User.Id.ToString()))
                {
                    DiscordButtonComponent shutdownButton = new(ButtonStyle.Danger, "shutdown-button", "Shut Down", true);
                    DiscordButtonComponent cancelButton = new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel", true);
                    e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new DiscordInteractionResponseBuilder().WithContent("Shutdown canceled.").AddComponents(shutdownButton, cancelButton));
                }
                else
                {
                    e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{e.User.Mention}, you are not authorized to perform this action!").AsEphemeral(true));
                }
            }
            if (e.Id == "view-dm-reply-info")
            {
                DiscordEmbedField channelIdField = e.Message.Embeds[0].Fields.Where(f => f.Name == "Channel ID").First();
                ulong channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

                DiscordEmbedField messageIdField = e.Message.Embeds[0].Fields.Where(f => f.Name == "Message ID").First();
                ulong messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                DiscordChannel channel = await s.GetChannelAsync(channelId);
                DiscordMessage message = await channel.GetMessageAsync(messageId);

                DiscordEmbedBuilder embed = new()
                {
                    Color = DiscordColor.Blurple,
                    Title = "DM Reply Info",
                    Description = $"{message.ReferencedMessage.Content}",
                };

                embed.AddField("Reply ID", $"`{message.ReferencedMessage.Id}`");
                embed.AddField("Target User ID", $"`{message.ReferencedMessage.Author.Id}`", true);
                embed.AddField("Target User Mention", $"{message.ReferencedMessage.Author.Mention}", true);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
            }
            if (e.Id == "view-dm-context")
            {
                DiscordEmbedField channelIdField = e.Message.Embeds[0].Fields.Where(f => f.Name == "Channel ID").First();
                ulong channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

                DiscordEmbedField messageIdField = e.Message.Embeds[0].Fields.Where(f => f.Name == "Message ID").First();
                ulong messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                DiscordChannel channel = await s.GetChannelAsync(channelId);
                DiscordMessage message = await channel.GetMessageAsync(messageId);

                string contextContent = "";
                ulong contextId = default;
                DiscordUser contextAuthor = default;
                DiscordMessage contextMsg = default;
                foreach (DiscordMessage msg in await channel.GetMessagesBeforeAsync(messageId, 1))
                {
                    contextContent = msg.Content;
                    contextId = msg.Id;
                    contextAuthor = msg.Author;
                    contextMsg = msg;
                    // There is only 1 message in the list we're enumerating here, but this makes sure the foreach only runs once to avoid issues just in case.
                    break;
                }

                if (string.IsNullOrWhiteSpace(contextContent) && contextMsg.Embeds != null)
                {
                    contextContent = "[Embed Content]\n" + contextMsg.Embeds[0].Description;
                }

                DiscordEmbedBuilder embed = new()
                {
                    Color = DiscordColor.Blurple,
                    Title = "DM Context Info",
                    Description = $"{contextContent}",
                };

                embed.AddField("Context Message ID", $"`{contextId}`");
                embed.AddField("Target User ID", $"`{contextAuthor.Id}`", true);
                embed.AddField("Target User Mention", $"{contextAuthor.Mention}", true);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral(true));
            }
            if (e.Id == "server-avatar-ctx-cmd-button")
            {
                Regex idPattern = new(@"\d+");
                ulong targetUserId = Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString());
                DiscordUser targetUser = await s.GetUserAsync(targetUserId);

                DiscordMember targetMember;
                try
                {
                    targetMember = await e.Guild.GetMemberAsync(targetUserId);
                }
                catch
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent("Hmm. It doesn't look like that user is in the server, so they don't have a server avatar.").AsEphemeral(true));
                    return;
                }

                if (targetMember.GuildAvatarUrl == null)
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"{targetUser.Mention} doesn't have a Server Avatar set! Try the User Avatar button to get their avatar.").AsEphemeral(true));
                    return;
                }

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Sure, the Server Avatar for {targetUser.Mention}. Here you go:\n{targetMember.GuildAvatarUrl.Replace("size=1024", "size=4096")}").AsEphemeral(true));
            }
            if (e.Id == "user-avatar-ctx-cmd-button")
            {
                Regex idPattern = new(@"\d+");
                ulong targetUserId = Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString());
                DiscordUser targetUser = await s.GetUserAsync(targetUserId);

                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource, new DiscordInteractionResponseBuilder().WithContent($"Sure, the User Avatar for {targetUser.Mention}. Here you go:\n{targetUser.AvatarUrl.Replace("size=1024", "size=4096")}").AsEphemeral(true));
            }
            if (e.Id == "code-quick-shortcut")
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                DiscordInteraction ctx = e.Interaction;
                try
                {
                    EventGlobals globals = new(Program.discord, ctx);

                    ScriptOptions scriptOptions = ScriptOptions.Default;
                    scriptOptions = scriptOptions.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.SlashCommands", "DSharpPlus.Interactivity", "Microsoft.Extensions.Logging");
                    scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                    Script<object> script = CSharpScript.Create(e.Message.Content, scriptOptions, typeof(EventGlobals));
                    script.Compile();
                    ScriptState<object> result = await script.RunAsync(globals).ConfigureAwait(false);

                    if (result == null)
                    {
                        await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("null"));
                    }
                    else
                    {
                        if (result.ReturnValue == null)
                        {
                            await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("null"));
                        }
                        else
                        {
                            if (string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                            {
                                // Isn't null, so it has to be whitespace
                                await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"\"{result.ReturnValue}\""));
                                return;
                            }

                            await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(result.ReturnValue.ToString()));
                        }
                    }
                }
                catch (Exception ex)
                {
                    await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(ex.GetType() + ": " + ex.Message).AsEphemeral(true));
                }
            }
        }

        public class EventGlobals
        {
            public DiscordMessage Message { get; set; }
            public DiscordChannel Channel { get; set; }
            public DiscordGuild Guild { get; set; }
            public DiscordUser User { get; set; }
            public DiscordMember Member { get; set; }
            public DiscordInteraction Context { get; set; }

            public DiscordClient Client;

            public EventGlobals(DiscordClient client, DiscordInteraction ctx)
            {
                Client = client;
                Channel = ctx.Channel;
                Guild = ctx.Guild;
                User = ctx.User;
                if (Guild != null)
                {
                    Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                Context = ctx;
            }
        }

        public static async Task MessageUpdated(DiscordClient client, MessageUpdateEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await Helpers.KeywordCheck(e.Message);
                }
                catch (Exception ex)
                {
                    await ThrowMessageException(ex, e.Message);
                }
            });
        }

        public static async Task MessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            await PerServerFeatures.Checks.MessageCreateChecks(e);

            Task.Run(async () =>
            {
                try
                {
                    await Helpers.KeywordCheck(e.Message);

                    if (!e.Channel.IsPrivate)
                        return;

                    if (e.Author.IsCurrent)
                        return;

                    if (client.CurrentApplication.Owners.Contains(e.Author) && e.Message.Content.StartsWith("sendto"))
                    {
                        Regex idPattern = new(@"[0-9]+");
                        string idMatch = idPattern.Match(e.Message.Content).ToString();
                        DiscordChannel targetChannel = default;
                        DiscordUser targetUser = default;
                        DiscordMember targetMember = default;
                        ulong targetId;

                        try
                        {
                            targetId = Convert.ToUInt64(idMatch);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent($"Hmm, that doesn't look like a valid ID! Make sure it's a user or channel ID!\n```\n{ex.GetType()}: {ex.Message}\n```")
                                .WithReply(e.Message.Id));
                            return;
                        }

                        try
                        {
                            targetChannel = await Program.discord.GetChannelAsync(targetId);
                        }
                        catch
                        {
                            try
                            {
                                targetUser = await Program.discord.GetUserAsync(targetId);
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                    .WithContent($"Hmm, I couldn't parse that ID! Make sure it's either a user ID or a channel ID and, if it's a channel ID, that I have permission to see the channel.\n```\n{ex.GetType()}: {ex.Message}\n```")
                                    .WithReply(e.Message.Id));
                                return;
                            }
                        }

                        if (targetChannel == default)
                        {
                            DiscordGuild mutualServer = default;
                            foreach (KeyValuePair<ulong, DiscordGuild> guildId in client.Guilds)
                            {
                                DiscordGuild server = await client.GetGuildAsync(guildId.Key);

                                if (server.Members.ContainsKey(targetUser.Id))
                                {
                                    mutualServer = await client.GetGuildAsync(server.Id);
                                    break;
                                }
                            }

                            try
                            {
                                targetMember = await mutualServer.GetMemberAsync(targetUser.Id);
                            }
                            catch (Exception ex)
                            {
                                await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                    .WithContent($"I tried to DM that user, but I don't have any mutual servers with them so Discord wouldn't let me send it. Sorry!\n```\n{ex.GetType()}: {ex.Message}\n```")
                                    .WithReply(e.Message.Id));
                                return;
                            }
                            targetChannel = await targetMember.CreateDmChannelAsync();
                        }

                        Regex getContentPattern = new(@"[0-9]+.*");
                        string content = getContentPattern.Match(e.Message.Content).ToString();
                        content = content.Replace(idMatch, "").Trim();

                        if (e.Message.Attachments.Any())
                        {
                            foreach (DiscordAttachment attachment in e.Message.Attachments)
                            {
                                content += $"\n{attachment.Url}";
                            }
                        }

                        DiscordMessage message;
                        try
                        {
                            message = await targetChannel.SendMessageAsync(content);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent($"Hmm, I couldn't send that message!\n```\n{ex.GetType()}: {ex.Message}\n```")
                                .WithReply(e.Message.Id));
                            return;
                        }

                        await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                            .WithContent($"Sent! (`{message.Id}` in `{message.Channel.Id}`)")
                            .WithReply(e.Message.Id));

                        return;
                    }

                    if (client.CurrentApplication.Owners.Contains(e.Author) && e.Message.ReferencedMessage != null && e.Message.ReferencedMessage.Author.IsCurrent && e.Message.ReferencedMessage.Embeds.Count != 0 && e.Message.ReferencedMessage.Embeds[0].Title.Contains("DM received from"))
                    {
                        // If these conditions are true, a bot owner has replied to a forwarded message. Now we need to forward that reply.

                        DiscordEmbedField userIdField = e.Message.ReferencedMessage.Embeds[0].Fields.Where(f => f.Name == "User ID").First();
                        ulong userId = Convert.ToUInt64(userIdField.Value.Replace("`", ""));

                        DiscordEmbedField mutualServersField = e.Message.ReferencedMessage.Embeds[0].Fields.Where(f => f.Name == "Mutual Servers").First();

                        Regex mutualIdPattern = new(@"[0-9]*;");
                        ulong firstMutualId = Convert.ToUInt64(mutualIdPattern.Match(mutualServersField.Value).ToString().Replace(";", "").Replace("`", ""));

                        DiscordGuild mutualServer = await client.GetGuildAsync(firstMutualId);
                        DiscordMember member = await mutualServer.GetMemberAsync(userId);

                        DiscordEmbedField messageIdField = e.Message.ReferencedMessage.Embeds[0].Fields.Where(f => f.Name == "Message ID").First();
                        ulong messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

                        string attachmentUrls = "";
                        string messageToSend = "";
                        if (e.Message.Attachments.Count != 0)
                        {
                            foreach (DiscordAttachment attachment in e.Message.Attachments)
                            {
                                attachmentUrls += $"{attachment.Url}\n";
                            }
                            messageToSend = $"{e.Message.Content}\n{attachmentUrls}";
                        }
                        else
                        {
                            messageToSend = e.Message.Content;
                        }

                        DiscordMessageBuilder replyBuilder = new DiscordMessageBuilder().WithContent(messageToSend).WithReply(messageId);

                        DiscordMessage reply = await member.SendMessageAsync(replyBuilder);

                        DiscordMessageBuilder messageBuilder = new DiscordMessageBuilder().WithContent($"Sent! (`{reply.Id}` in `{reply.Channel.Id}`)").WithReply(e.Message.Id);
                        await e.Channel.SendMessageAsync(messageBuilder);
                    }
                    else
                    {
                        try
                        {
                            foreach (DiscordUser owner in client.CurrentApplication.Owners)
                            {
                                foreach (KeyValuePair<ulong, DiscordGuild> guildPair in client.Guilds)
                                {
                                    DiscordGuild guild = await client.GetGuildAsync(guildPair.Key);

                                    if (guild.Members.ContainsKey(owner.Id))
                                    {
                                        DiscordMember ownerMember = await guild.GetMemberAsync(owner.Id);

                                        DiscordEmbedBuilder embed = new()
                                        {
                                            Color = DiscordColor.Yellow,
                                            Title = $"DM received from {e.Author.Username}#{e.Author.Discriminator}!",
                                            Description = $"{e.Message.Content}",
                                            Timestamp = DateTime.UtcNow
                                        };

                                        embed.AddField("User ID", $"`{e.Author.Id}`", true);
                                        embed.AddField("User Mention", $"{e.Author.Mention}", true);
                                        embed.AddField("User Avatar URL", $"[Link]({e.Author.AvatarUrl})", true);
                                        embed.AddField("Channel ID", $"`{e.Channel.Id}`", true);
                                        embed.AddField("Message ID", $"`{e.Message.Id}`", true);

                                        string attachmentUrls = "";
                                        if (e.Message.Attachments.Count != 0)
                                        {
                                            foreach (DiscordAttachment attachment in e.Message.Attachments)
                                            {
                                                attachmentUrls += $"{attachment.Url}\n";
                                            }
                                            embed.AddField("Attachments", attachmentUrls, true);
                                        }

                                        string mutualServers = "";

                                        foreach (KeyValuePair<ulong, DiscordGuild> guildId in client.Guilds)
                                        {
                                            DiscordGuild server = await client.GetGuildAsync(guildId.Key);

                                            if (server.Members.ContainsKey(e.Author.Id))
                                            {
                                                mutualServers += $"- `{server}`\n";
                                            }
                                        }

                                        DiscordMessageBuilder messageBuilder = new();

                                        string isReply = "No";
                                        if (e.Message.ReferencedMessage != null)
                                        {
                                            isReply = "Yes";
                                            DiscordButtonComponent button = new(ButtonStyle.Primary, "view-dm-reply-info", "View Reply Info");
                                            messageBuilder = messageBuilder.AddComponents(button);
                                        }
                                        embed.AddField("Is Reply", isReply);

                                        IReadOnlyList<DiscordMessage> messages = await e.Channel.GetMessagesBeforeAsync(e.Message.Id);
                                        bool contextExists = false;
                                        foreach (DiscordMessage msg in messages)
                                        {
                                            if (msg.Content != null)
                                            {
                                                contextExists = true;
                                            }
                                        }

                                        if (contextExists)
                                        {
                                            DiscordButtonComponent button = new(ButtonStyle.Primary, "view-dm-context", "View Context");
                                            messageBuilder.AddComponents(button);
                                        }

                                        embed.AddField("Mutual Servers", mutualServers, false);

                                        messageBuilder = messageBuilder.AddEmbed(embed.Build());

                                        await ownerMember.SendMessageAsync(messageBuilder);
                                        return;
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[{DateTime.Now}] A DM was received, but could not be forwarded!\nException Details: {ex.GetType}: {ex.Message}\nMessage Content: {e.Message.Content}");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    await ThrowMessageException(ex, e.Message);
                }
            });
        }

        static async Task ThrowMessageException(Exception ex, DiscordMessage message)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = DiscordColor.Red,
                Title = "An exception occurred when processing a message event",
                Description = $"`{ex.GetType()}` occurred when processing [this message]({message.JumpLink}) (message `{message.Id}` in channel `{message.Channel.Id}`)."
            };
            embed.AddField("Message", $"{ex.Message}");
            embed.AddField("Debug Info", $"If you'd like to contact the bot owner about this, include this debug info:\n```\n{ex}\n```");

            await Program.homeChannel.SendMessageAsync(embed);
        }
    }
}

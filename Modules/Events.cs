namespace MechanicalMilkshake.Modules
{
    public class Events
    {
        public class Errors
        {
            public static async Task SlashCommandErrored(SlashCommandsExtension scmds, SlashCommandErrorEventArgs e)
            {
                if (e.Exception is SlashExecutionChecksFailedException)
                {
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
                    if (ex.GetType().ToString() == "System.ArgumentException")
                    {
                        embed.AddField("What's that mean?", "This usually means that you used the command incorrectly.\n" +
                            $"Please run `help {e.Command.QualifiedName}` for information about what you need to provide for the `{e.Command.QualifiedName}` command.");
                    }
                    if (e.Command.QualifiedName == "upload")
                    {
                        embed.AddField("Did you forget to include a file name?", "`upload` requires that you specify a name for the file as the first argument. If you'd like to use a randomly-generated file name, use the name `random`.");
                    }
                    if (e.Command.QualifiedName == "tellraw" && ex.GetType().ToString() == "System.ArgumentException")
                    {
                        await e.Context.RespondAsync("An error occurred! Either you did not specify a target channel, or I do not have permission to see that channel.");
                    }
                    if (ex.GetType().ToString() == "System.InvalidOperationException" && ex.Message.Contains("this group is not executable"))
                    {
                        await e.Context.RespondAsync($"Did you mean to run a command inside this group? `{e.Command.QualifiedName}` cannot be run on its own. Try `help {e.Command.QualifiedName}` to see what commands are available.");
                    }
                    else
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
            if (e.Id == "shutdown-button")
            {
                Owner.DebugCmds.ShutdownConfirmed(e.Interaction);
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
                    Title = $"DM Reply Info",
                    Description = $"{message.ReferencedMessage.Content}",
                };

                embed.AddField("Reply ID", $"`{message.ReferencedMessage.Id}`");
                embed.AddField("Target User ID", $"`{message.ReferencedMessage.Author.Id}`", true);
                embed.AddField("Target User Mention", $"{message.ReferencedMessage.Author.Mention}", true);

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
                    var globals = new EventGlobals(Program.discord, ctx);

                    var scriptOptions = ScriptOptions.Default;
                    scriptOptions = scriptOptions.WithImports("System", "System.Collections.Generic", "System.Linq", "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.SlashCommands", "DSharpPlus.Interactivity", "Microsoft.Extensions.Logging");
                    scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies().Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                    var script = CSharpScript.Create(e.Message.Content, scriptOptions, typeof(EventGlobals));
                    script.Compile();
                    var result = await script.RunAsync(globals).ConfigureAwait(false);

                    if (result != null && result.ReturnValue != null && !string.IsNullOrWhiteSpace(result.ReturnValue.ToString()))
                    {
                        await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"{result.ReturnValue}").AsEphemeral(true));
                    }
                    else
                    {
                        await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent($"Eval was successful, but there was nothing returned."));
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

        public static async Task MessageCreated(DiscordClient client, MessageCreateEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    if (!e.Channel.IsPrivate)
                        return;

                    if (e.Author.IsCurrent)
                        return;

                    if (client.CurrentApplication.Owners.Contains(e.Author) && e.Message.Content.StartsWith("sendto"))
                    {
                        Regex idPattern = new(@"[0-9]+");
                        string idMatch = idPattern.Match(e.Message.Content).ToString();
                        DiscordChannel targetChannel;
                        try
                        {
                            ulong channelId = Convert.ToUInt64(idMatch);
                            targetChannel = await Program.discord.GetChannelAsync(channelId);
                        }
                        catch (Exception ex)
                        {
                            await e.Channel.SendMessageAsync(new DiscordMessageBuilder()
                                .WithContent($"Hmm, I couldn't parse the channel ID in your message! Make sure it's a channel ID and that I have permission to see the channel!\n```\n{ex.GetType()}: {ex.Message}\n```")
                                .WithReply(e.Message.Id));
                            return;
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
                            foreach (var attachment in e.Message.Attachments)
                            {
                                attachmentUrls += $"{attachment.Url}\n";
                            }
                            messageToSend = $"{e.Message.Content}\n{attachmentUrls}";
                        }
                        else
                        {
                            messageToSend = e.Message.Content;
                        }

                        var replyBuilder = new DiscordMessageBuilder().WithContent(messageToSend).WithReply(messageId);

                        DiscordMessage reply = await member.SendMessageAsync(replyBuilder);

                        var messageBuilder = new DiscordMessageBuilder().WithContent($"Sent! (`{reply.Id}` in `{reply.Channel.Id}`)").WithReply(e.Message.Id);
                        await e.Channel.SendMessageAsync(messageBuilder);
                    }
                    else
                    {
                        try
                        {
                            foreach (DiscordUser owner in client.CurrentApplication.Owners)
                            {
                                foreach (var guildPair in client.Guilds)
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
                                            foreach (var attachment in e.Message.Attachments)
                                            {
                                                attachmentUrls += $"{attachment.Url}\n";
                                            }
                                            embed.AddField("Attachments", attachmentUrls, true);
                                        }

                                        string mutualServers = "";

                                        foreach (var guildId in client.Guilds)
                                        {
                                            DiscordGuild server = await client.GetGuildAsync(guildId.Key);

                                            if (server.Members.ContainsKey(e.Author.Id))
                                            {
                                                mutualServers += $"- `{server}`\n";
                                            }
                                        }

                                        var messageBuilder = new DiscordMessageBuilder();

                                        string isReply = "No";
                                        if (e.Message.ReferencedMessage != null)
                                        {
                                            isReply = "Yes";
                                            DiscordButtonComponent button = new(ButtonStyle.Primary, "view-dm-reply-info", "View Reply Info");
                                            messageBuilder = messageBuilder.AddComponents(button);
                                        }
                                        embed.AddField("Is Reply", isReply);

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
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = DiscordColor.Red,
                        Title = "An exception occurred when processing a message event",
                        Description = $"`{ex.GetType()}` occurred when processing [this message]({e.Message.JumpLink}) (message `{e.Message.Id}` in channel `{e.Message.Channel.Id}`)."
                    };
                    embed.AddField("Message", $"{ex.Message}");
                    embed.AddField("Debug Info", $"If you'd like to contact the bot owner about this, include this debug info:\n```\n{ex}\n```");

                    await Program.homeChannel.SendMessageAsync(embed);
                }
            });
        }
    }
}

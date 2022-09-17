namespace MechanicalMilkshake.Events;

public class ComponentInteractionEvent
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static async Task ComponentInteractionCreated(DiscordClient s, ComponentInteractionCreateEventArgs e)
    {
        if (e.Id == "slash-fail-restart-button")
        {
            if (Program.configjson.Base.AuthorizedUsers.Contains(e.User.Id.ToString()))
            {
                DiscordButtonComponent restartButton =
                    new(ButtonStyle.Danger, "slash-fail-restart-button", "Restart", true);

                try
                {
                    var dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                    if (string.IsNullOrWhiteSpace(dockerCheckFile))
                    {
                        await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
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
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                        new DiscordInteractionResponseBuilder()
                            .WithContent(
                                "The bot may not be running under Docker and as such cannot be restarted this way! Please restart the bot manually.")
                            .AddComponents(restartButton));
                    return;
                }

                e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Restarting!")
                        .AddComponents(restartButton));
                Environment.Exit(1);
            }
            else
            {
                e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                        .AsEphemeral());
            }
        }
        else if (e.Id == "shutdown-button")
        {
            if (Program.configjson.Base.AuthorizedUsers.Contains(e.User.Id.ToString()))
            {
                DiscordButtonComponent shutdownButton =
                    new(ButtonStyle.Danger, "shutdown-button", "Shut Down", true);
                DiscordButtonComponent cancelButton =
                    new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel", true);
                e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .WithContent("**Warning: The bot is now shutting down. This action is permanent.**")
                        .AddComponents(shutdownButton, cancelButton));

                Program.discord.DisconnectAsync();
                Environment.Exit(0);
            }
            else
            {
                e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                        .AsEphemeral());
            }
        }
        else if (e.Id == "shutdown-cancel-button")
        {
            if (Program.configjson.Base.AuthorizedUsers.Contains(e.User.Id.ToString()))
            {
                DiscordButtonComponent shutdownButton =
                    new(ButtonStyle.Danger, "shutdown-button", "Shut Down", true);
                DiscordButtonComponent cancelButton =
                    new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel", true);
                e.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder().WithContent("Shutdown canceled.")
                        .AddComponents(shutdownButton, cancelButton));
            }
            else
            {
                e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent($"{e.User.Mention}, you are not authorized to perform this action!")
                        .AsEphemeral());
            }
        }
        else if (e.Id == "view-dm-reply-info")
        {
            var channelIdField =
                e.Message.Embeds[0].Fields.Where(f => f.Name == "Channel ID").First();
            var channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

            var messageIdField =
                e.Message.Embeds[0].Fields.Where(f => f.Name == "Message ID").First();
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

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral());
        }
        else if (e.Id == "view-dm-context")
        {
            var channelIdField =
                e.Message.Embeds[0].Fields.Where(f => f.Name == "Channel ID").First();
            var channelId = Convert.ToUInt64(channelIdField.Value.Replace("`", ""));

            var messageIdField =
                e.Message.Embeds[0].Fields.Where(f => f.Name == "Message ID").First();
            var messageId = Convert.ToUInt64(messageIdField.Value.Replace("`", ""));

            var channel = await s.GetChannelAsync(channelId);
            var message = await channel.GetMessageAsync(messageId);

            var contextContent = "";
            ulong contextId = default;
            DiscordUser contextAuthor = default;
            DiscordMessage contextMsg = default;
            foreach (var msg in await channel.GetMessagesBeforeAsync(messageId, 1))
            {
                contextContent = msg.Content;
                contextId = msg.Id;
                contextAuthor = msg.Author;
                contextMsg = msg;
                // There is only 1 message in the list we're enumerating here, but this makes sure the foreach only runs once to avoid issues just in case.
                break;
            }

            if (string.IsNullOrWhiteSpace(contextContent) && contextMsg.Embeds != null)
                contextContent = "[Embed Content]\n" + contextMsg.Embeds[0].Description;

            DiscordEmbedBuilder embed = new()
            {
                Color = DiscordColor.Blurple,
                Title = "DM Context Info",
                Description = $"{contextContent}"
            };

            embed.AddField("Context Message ID", $"`{contextId}`");
            embed.AddField("Target User ID", $"`{contextAuthor.Id}`", true);
            embed.AddField("Target User Mention", $"{contextAuthor.Mention}", true);

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AddEmbed(embed.Build()).AsEphemeral());
        }
        else if (e.Id == "server-avatar-ctx-cmd-button")
        {
            Regex idPattern = new(@"\d+");
            var targetUserId = Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString());
            var targetUser = await s.GetUserAsync(targetUserId);

            DiscordMember targetMember;
            try
            {
                targetMember = await e.Guild.GetMemberAsync(targetUserId);
            }
            catch
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            "Hmm. It doesn't look like that user is in the server, so they don't have a server avatar.")
                        .AsEphemeral());
                return;
            }

            if (targetMember.GuildAvatarUrl == null)
            {
                await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            $"{targetUser.Mention} doesn't have a Server Avatar set! Try using the User Avatar button to get their avatar.")
                        .AsEphemeral());
                return;
            }

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{targetMember.GuildAvatarUrl.Replace("size=1024", "size=4096")}")
                    .AsEphemeral());
        }
        else if (e.Id == "user-avatar-ctx-cmd-button")
        {
            Regex idPattern = new(@"\d+");
            var targetUserId = Convert.ToUInt64(idPattern.Match(e.Message.Content).ToString());
            var targetUser = await s.GetUserAsync(targetUserId);

            await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder()
                    .WithContent($"{targetUser.AvatarUrl.Replace("size=1024", "size=4096")}").AsEphemeral());
        }
        else if (e.Id == "code-quick-shortcut")
        {
            await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var ctx = e.Interaction;
            try
            {
                EventGlobals globals = new(Program.discord, ctx);

                var scriptOptions = ScriptOptions.Default;
                scriptOptions = scriptOptions.WithImports("System", "System.Collections.Generic", "System.Linq",
                    "System.Text", "System.Threading.Tasks", "DSharpPlus", "DSharpPlus.SlashCommands",
                    "DSharpPlus.Interactivity", "Microsoft.Extensions.Logging");
                scriptOptions = scriptOptions.WithReferences(AppDomain.CurrentDomain.GetAssemblies()
                    .Where(xa => !xa.IsDynamic && !string.IsNullOrWhiteSpace(xa.Location)));

                var script = CSharpScript.Create(e.Message.Content, scriptOptions, typeof(EventGlobals));
                script.Compile();
                var result = await script.RunAsync(globals).ConfigureAwait(false);

                if (result == null)
                {
                    await ctx.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("null"));
                }
                else
                {
                    if (result.ReturnValue == null)
                    {
                        await ctx.CreateFollowupMessageAsync(
                            new DiscordFollowupMessageBuilder().WithContent("null"));
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
        }
        else if (e.Id == "clear-confirm-callback")
        {
            Task.Run(async () =>
            {
                var messagesToClear = Clear.MessagesToClear;

                if (!messagesToClear.ContainsKey(e.Message.Id))
                {
                    await e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent("These messages have already been deleted!")
                            .AsEphemeral());
                    return;
                }

                await e.Interaction.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder().AsEphemeral());

                var messages = messagesToClear.GetValueOrDefault(e.Message.Id);

                await e.Channel.DeleteMessagesAsync(messages, $"[Clear by {e.User.Username}#{e.User.Discriminator}]");

                messagesToClear.Remove(e.Message.Id);

                await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Done!")
                    .AsEphemeral());
            });
        }
        else
        {
            e.Interaction.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                    "Unknown interaction ID! Contact the bot developer for assistance.").AsEphemeral());
        }
    }
}

public class EventGlobals
{
    public DiscordClient Client;

    public EventGlobals(DiscordClient client, DiscordInteraction ctx)
    {
        Client = client;
        Channel = ctx.Channel;
        Guild = ctx.Guild;
        User = ctx.User;
        if (Guild != null) Member = Guild.GetMemberAsync(User.Id).ConfigureAwait(false).GetAwaiter().GetResult();

        Context = ctx;
    }

    public DiscordMessage Message { get; set; }
    public DiscordChannel Channel { get; set; }
    public DiscordGuild Guild { get; set; }
    public DiscordUser User { get; set; }
    public DiscordMember Member { get; set; }
    public DiscordInteraction Context { get; set; }
}
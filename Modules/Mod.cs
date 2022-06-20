using DSharpPlus.SlashCommands.Attributes;

namespace MechanicalMilkshake.Modules
{
    public class Mod : ApplicationCommandModule
    {
        [SlashCommandGroup("lockdown", "Lock or unlock a channel. Requires the Timeout Members permission.")]
        [SlashRequirePermissions(Permissions.ModerateMembers)]
        public class Lockdown
        {
            [SlashCommand("lock", "Lock a channel to prevent members from sending messages. Requires the Timeout Members permission.")]
            public async Task Lock(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                var existingOverwrites = ctx.Channel.PermissionOverwrites.ToArray();

                await ctx.Channel.AddOverwriteAsync(ctx.Member, Permissions.SendMessages, Permissions.None);
                await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id), Permissions.SendMessages, Permissions.None);
                await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages);

                foreach (var overwrite in existingOverwrites)
                {
                    if (overwrite.Type == OverwriteType.Role)
                    {
                        if (await overwrite.GetRoleAsync() == ctx.Guild.EveryoneRole)
                        {
                            await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed, Permissions.SendMessages | overwrite.Denied);
                        }
                        else
                        {
                            await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed, overwrite.Denied);

                        }
                    }
                    else
                    {
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetMemberAsync(), overwrite.Allowed, overwrite.Denied);
                    }
                }

                await ctx.Channel.SendMessageAsync("This channel has been locked.");

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Channel locked successfully.").AsEphemeral(true));
            }

            [SlashCommand("unlock", "Unlock a locked channel to allow members to send messages. Requires the Timeout Members permission.")]
            public async Task Unlock(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                foreach (var permission in ctx.Channel.PermissionOverwrites.ToArray())
                {
                    if (permission.Type == OverwriteType.Role)
                    {
                        if (await permission.GetRoleAsync() == ctx.Guild.EveryoneRole && permission.Denied.HasPermission(Permissions.SendMessages))
                        {
                            DiscordOverwriteBuilder newOverwrite = new(ctx.Guild.EveryoneRole)
                            {
                                Allowed = permission.Allowed,
                                Denied = (Permissions)(permission.Denied - Permissions.SendMessages)
                            };

                            await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, newOverwrite.Allowed, newOverwrite.Denied);
                        }
                    }
                    else
                    {
                        if (await permission.GetMemberAsync() == await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id) || await permission.GetMemberAsync() == ctx.Member)
                        {
                            await permission.DeleteAsync();
                        }
                    }
                }

                await ctx.Channel.SendMessageAsync("This channel has been unlocked.");
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Channel unlocked successfully.").AsEphemeral(true));
            }
        }

        [SlashCommand("tellraw", "Speak through the bot! Requires the Kick Members, Ban Members or Timeout Members permissions.")]
        public async Task Tellraw(InteractionContext ctx, [Option("message", "The message to have the bot send.")] string message, [Option("channel", "The channel to send the message in.")] DiscordChannel channel = null)
        {
            if (!ctx.Member.Permissions.HasPermission(Permissions.KickMembers) && !ctx.Member.Permissions.HasPermission(Permissions.BanMembers) && !ctx.Member.Permissions.HasPermission(Permissions.ModerateMembers) && !Program.configjson.AuthorizedUsers.Contains(ctx.User.Id.ToString()))
            {
                throw new SlashExecutionChecksFailedException();
            }

            DiscordChannel targetChannel;
            if (channel != null)
            {
                targetChannel = channel;
            }
            else
            {
                targetChannel = ctx.Channel;
            }

            try
            {
                await targetChannel.SendMessageAsync(message);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("I don't have permission to send messages in that channel!").AsEphemeral(true));
                return;
            }
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"I sent your message to {targetChannel.Mention}.").AsEphemeral(true));
        }

        [SlashCommand("clear", "Delete a given number of messages from a channel. Requires the Manage Messages permission.")]
        [SlashRequirePermissions(Permissions.ManageMessages)]
        [SlashRequireGuild]
        public async Task Clear(InteractionContext ctx, [Option("count", "The number of messages to delete.")] long count)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));
            IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync((int)count);
            await ctx.Channel.DeleteMessagesAsync(messages);
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Deleted {messages.Count} messages!").AsEphemeral(true));
        }

        [SlashCommand("kick", "Kick a user. They can rejoin the server with an invite. Requires the Kick Members permission.")]
        [SlashRequirePermissions(Permissions.KickMembers)]
        [SlashRequireGuild]
        public async Task Kick(InteractionContext ctx, [Option("user", "The user to kick.")] DiscordUser userToKick, [Option("reason", "The reason for the kick.")] string reason = "No reason provided.")
        {
            DiscordMember memberToKick;
            try
            {
                memberToKick = await ctx.Guild.GetMemberAsync(userToKick.Id);
            }
            catch
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hmm, **{userToKick.Username}#{userToKick.Discriminator}** doesn't seem to be in the server.").AsEphemeral(true));
                return;
            }
            try
            {
                await memberToKick.RemoveAsync(reason);
            }
            catch
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Something went wrong. You or I may not be allowed to kick **{userToKick.Username}#{userToKick.Discriminator}**! Please check the role hierarchy and permissions.").AsEphemeral(true));
                return;
            }
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("User kicked successfully.").AsEphemeral(true));
            await ctx.Channel.SendMessageAsync($"{userToKick.Mention} has been kicked: **{reason}**");
        }

        [SlashCommand("ban", "Ban a user. They will not be able to rejoin unless unbanned. Requires the Ban Members permission.")]
        [SlashRequirePermissions(Permissions.BanMembers)]
        [SlashRequireGuild]
        public async Task Ban(InteractionContext ctx, [Option("user", "The user to ban.")] DiscordUser userToBan, [Option("reason", "The reason for the ban.")] string reason = "No reason provided.")
        {
            try
            {
                await ctx.Guild.BanMemberAsync(userToBan.Id, 0, reason);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Something went wrong. You or I may not be allowed to ban **{userToBan.Username}#{userToBan.Discriminator}**! Please check the role hierarchy and permissions.").AsEphemeral(true));
                return;
            }
            catch (Exception e)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hmm, something went wrong while trying to ban that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```").AsEphemeral(true));
                return;
            }
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("User banned successfully.").AsEphemeral(true));
            await ctx.Channel.SendMessageAsync($"{userToBan.Mention} has been banned: **{reason}**");
        }

        [SlashCommand("unban", "Unban a user. Requires the Ban Members permission.")]
        [SlashRequirePermissions(Permissions.BanMembers)]
        [SlashRequireGuild]
        public async Task Unban(InteractionContext ctx, [Option("user", "The user to unban.")] DiscordUser userToUnban)
        {
            try
            {
                await ctx.Guild.UnbanMemberAsync(userToUnban);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Something went wrong. You or I may not be allowed to unban **{userToUnban.Username}#{userToUnban.Discriminator}**! Please check the role hierarchy and permissions.").AsEphemeral(true));
                return;
            }
            catch (Exception e)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hmm, something went wrong while trying to unban that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```").AsEphemeral(true));
                return;
            }
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Successfully unbanned **{userToUnban.Username}#{userToUnban.Discriminator}**!"));
        }

        [SlashCommandGroup("timeout", "Set or clear a timeout for a user. Requires the Timeout Members permission.")]
        [SlashRequirePermissions(Permissions.ModerateMembers)]
        [SlashRequireGuild]
        public class TimeoutCmds
        {
            [SlashCommand("set", "Time out a user. Requires the Timeout Members permission.")]
            public async Task SetTimeout(InteractionContext ctx, [Option("member", "The member to time out.")] DiscordUser user, [Option("duration", "How long the timeout should last. Maximum value is 28 days due to Discord limitations.")] string duration, [Option("reason", "The reason for the timeout.")] string reason = "No reason provided.")
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder().AsEphemeral(true));

                DiscordMember member;

                try
                {
                    member = await ctx.Guild.GetMemberAsync(user.Id);
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Hmm. It doesn't look like that user is in the server, so I can't time them out.").AsEphemeral(true));
                    return;
                }

                TimeSpan parsedDuration = HumanDateParser.HumanDateParser.Parse(duration).Subtract(ctx.Interaction.CreationTimestamp.DateTime);
                DateTime expireTime = ctx.Interaction.CreationTimestamp.DateTime + parsedDuration;

                try
                {
                    await member.TimeoutAsync(expireTime, reason);
                }
                catch (DSharpPlus.Exceptions.BadRequestException)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("It looks like you tried to set the timeout duration to more than 28 days in the future! Due to Discord limitations, timeouts can only be up to 28 days.").AsEphemeral(true));
                    return;
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Something went wrong. You or I may not be allowed to time out **{user.Username}#{user.Discriminator}**! Please check the role hierarchy and permissions.").AsEphemeral(true));
                    return;
                }
                catch (Exception e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Hmm, something went wrong while trying to time out that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```").AsEphemeral(true));
                    return;
                }

                DateTime dateToConvert = Convert.ToDateTime(expireTime);
                long unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Successfully timed out {user.Mention} until `{expireTime}` (<t:{unixTime}:R>)!").AsEphemeral(true));
                await ctx.Channel.SendMessageAsync($"{user.Mention} has been timed out, expiring <t:{unixTime}:R>: **{reason}**");
            }

            [SlashCommand("clear", "Clear a timeout before it's set to expire. Requires the Timeout Members permission.")]
            public async Task ClearTimeout(InteractionContext ctx, [Option("member", "The member whose timeout to clear.")] DiscordUser user)
            {
                await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource, new DiscordInteractionResponseBuilder());

                DiscordMember member;

                try
                {
                    member = await ctx.Guild.GetMemberAsync(user.Id);
                }
                catch
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Hmm. It doesn't look like that user is in the server, so I can't remove their timeout."));
                    return;
                }

                try
                {
                    await member.TimeoutAsync(null, "Timeout cleared manually.");
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Something went wrong. You or I may not be allowed to clear the timeout for **{user.Username}#{user.Discriminator}**! Please check the role hierarchy and permissions."));
                    return;
                }
                catch (Exception e)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Hmm, something went wrong while trying to clear the timeout for that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```").AsEphemeral(true));
                    return;
                }

                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent($"Successfully cleared the timeout for {user.Mention}!"));
            }
        }

        [SlashCommand("nickname", "Changes my nickname. Requires that you have the Manage Nicknames or Manage Server permissions.")]
        [SlashRequireGuild]
        public async Task Nickname(InteractionContext ctx, [Option("nickname", "What to change my nickname to. Leave this blank to clear it.")] string nickname = null)
        {
            // Checking permissions this way instead of using the [SlashRequireUserPermissions] attribute because I don't believe there's a way to check whether a user has one permission OR another permission with that attribute.
            if (!ctx.Member.Permissions.HasPermission(Permissions.ManageNicknames) && !ctx.Member.Permissions.HasPermission(Permissions.ManageGuild))
            {
                throw new SlashExecutionChecksFailedException();
            }

            DiscordMember bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
            await bot.ModifyAsync(x =>
            {
                x.Nickname = nickname;
                x.AuditLogReason = $"Nickname changed by {ctx.User.Username} (ID: {ctx.User.Id}).";
            });

            if (nickname != null)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Nickname changed to **{nickname}** successfully!"));
            }
            else
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Nickname cleared successfully!"));
            }
        }
    }
}

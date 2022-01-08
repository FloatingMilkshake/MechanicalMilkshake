using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
    public class Mod : BaseCommandModule
    {
        [Command("tellraw")]
        [Description("Speak through the bot! Requires the `Kick Members` permission.")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Tellraw(CommandContext ctx, [Description("The message to have the bot send. **If you'd like the message to be sent in a different channel than the one you're sending this command in, put the channel first, followed by a space.**"), RemainingText] string message)
        {
            DiscordChannel targetChannel = default;
            string extractedChannel = null;

            if (message.Contains(' '))
            {
                extractedChannel = message[..message.IndexOf(" ")];
                if (extractedChannel.Contains("<#"))
                {
                    extractedChannel = extractedChannel.Replace("<#", "");
                }
                if (extractedChannel.Contains('>'))
                {
                    extractedChannel = extractedChannel.Replace(">", "");
                }

                ulong targetChannelId = default;
                try
                {
                    targetChannelId = Convert.ToUInt64(extractedChannel);
                }
                catch
                {
                    await ctx.RespondAsync("Hmm, something went wrong trying to read the target channel from your message! Please try again or contact the bot developer.");
                    return;
                }

                targetChannel = await ctx.Client.GetChannelAsync(targetChannelId);

                message = message.Replace(extractedChannel, "");
                if (message.Contains("<#"))
                {
                    message = message.Replace("<#", "");
                }
                if (message.Contains('>'))
                {
                    message = message.Replace(">", "");
                }
            }
            else
            {
                targetChannel = ctx.Channel;
            }

            if (targetChannel != ctx.Channel)
            {
                try
                {
                    await targetChannel.SendMessageAsync(message);
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    await ctx.RespondAsync("I don't have permission to send messages in that channel!");
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception {e.Message} occurred when {ctx.User.Id} ran {ctx.Command.Name} in #{ctx.Channel.Name} (target channel: {targetChannel.Name})!\nException Details: {e}");
                    await ctx.RespondAsync("Something went wrong when attempting to send that message! This error has been logged.");
                    return;
                }
                if (ctx.Channel.IsPrivate)
                {
                    await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
                }
                else
                {
                    try
                    {
                        await ctx.RespondAsync($"I sent your message to {targetChannel.Mention}.");
                    }
                    catch
                    {
                        try
                        {
                            await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":white_check_mark:"));
                        }
                        catch
                        {
                            // Do nothing because this isn't really important
                        }
                    }
                }
            }
            else // if target channel is the same channel in which the command was invoked
            {
                try
                {
                    await ctx.Message.DeleteAsync();
                }
                catch
                {
                    // Do nothing; this isn't really a big deal. Maybe this will be changed later.
                }
                try
                {
                    await targetChannel.SendMessageAsync(message);
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    try
                    {
                        await ctx.Message.CreateReactionAsync(DiscordEmoji.FromName(ctx.Client, ":x:"));
                    }
                    catch
                    {
                        // Something went wrong while trying to send the message AND add the reaction. For now we're just going to do nothing. This may be changed later because silently failing isn't great.
                    }
                    return;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Exception {e.Message} occurred when {ctx.User.Id} ran {ctx.Command.Name} in #{ctx.Channel.Name} (target channel: {targetChannel.Name})!\nException Details: {e}");
                    await ctx.RespondAsync("Something went wrong when attempting to send that message! This error has been logged.");
                    return;
                }
            }
        }

        [Command("clear")]
        [Aliases("purge")]
        [Description("Delete a given number of messages from a channel. Requires the `Manage Messages` permission.")]
        [RequirePermissions(Permissions.ManageMessages)]
        [RequireGuild]
        public async Task Clear(CommandContext ctx, [Description("The number of messages to delete.")] int count)
        {
            await ctx.Message.DeleteAsync();
            System.Collections.Generic.IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync(count);
            await ctx.Channel.DeleteMessagesAsync(messages);
            DiscordMessage response = await ctx.Channel.SendMessageAsync($"Deleted {messages.Count} messages!");
            await Task.Delay(3000);
            await response.DeleteAsync();
        }

        [Command("kick")]
        [Aliases("yeet")]
        [Description("Kick a user. They can rejoin the server if they have an invite. Requires the `Kick Members` permission.")]
        [RequirePermissions(Permissions.KickMembers)]
        [RequireGuild]
        public async Task Kick(CommandContext ctx, [Description("The user to kick.")] DiscordUser userToKick, [Description("The reason for the kick."), RemainingText] string reason = "No reason provided.")
        {
            DiscordMember memberToKick;
            try
            {
                memberToKick = await ctx.Guild.GetMemberAsync(userToKick.Id);
            }
            catch
            {
                await ctx.RespondAsync($"Hmm, **{userToKick.Username}#{userToKick.Discriminator}** doesn't seem to be in the server.");
                return;
            }
            try
            {
                await memberToKick.RemoveAsync(reason);
            }
            catch
            {
                await ctx.RespondAsync($"Something went wrong. You or I may not be allowed to kick **{userToKick.Username}#{userToKick.Discriminator}**! Please check the role hierarchy and permissions.");
                return;
            }
            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync($"{userToKick.Mention} has been kicked: **{reason}**");
        }

        [Command("ban")]
        [Aliases("bonk")]
        [Description("Ban a user. They will not be able to rejoin unless unbanned. Requires the `Ban Members` permission.")]
        [RequirePermissions(Permissions.BanMembers)]
        [RequireGuild]
        public async Task Ban(CommandContext ctx, [Description("The user to ban.")] DiscordUser userToBan, [Description("The reason for the ban."), RemainingText] string reason = "No reason provided.")
        {
            try
            {
                await ctx.Guild.BanMemberAsync(userToBan.Id, 0, reason);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                await ctx.RespondAsync($"Something went wrong. You or I may not be allowed to ban **{userToBan.Username}#{userToBan.Discriminator}**! Please check the role hierarchy and permissions.");
                return;
            }
            catch (Exception e)
            {
                await ctx.RespondAsync("Something went wrong! I ran into a problem while trying to ban that user. This error has been logged.");
                Console.WriteLine($"ERROR: {e.GetType()} occurred when {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}) attempted to ban {userToBan.Username}#{userToBan.Discriminator} ({userToBan.Id}) from {ctx.Guild.Name}!");
            }
            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync($"{userToBan.Mention} has been banned: **{reason}**");
        }

        [Command("unban")]
        [Description("Unban a user. Requires the `Ban Members` permission.")]
        [RequirePermissions(Permissions.BanMembers)]
        [RequireGuild]
        public async Task Unban(CommandContext ctx, [Description("The user to unban.")] DiscordUser userToUnban)
        {
            try
            {
                await ctx.Guild.UnbanMemberAsync(userToUnban);
            }
            catch (DSharpPlus.Exceptions.UnauthorizedException)
            {
                await ctx.RespondAsync($"Something went wrong. You or I may not be allowed to unban **{userToUnban.Username}#{userToUnban.Discriminator}**! Please check the role hierarchy and permissions.");
                return;
            }
            catch (Exception e)
            {
                await ctx.RespondAsync("Something went wrong! I ran into a problem while trying to unban that user. This error has been logged.");
                Console.WriteLine($"ERROR: {e.GetType()} occurred when {ctx.Member.Username}#{ctx.Member.Discriminator} ({ctx.Member.Id}) attempted to unban {userToUnban.Username}#{userToUnban.Discriminator} ({userToUnban.Id}) from {ctx.Guild.Name}!");
            }
            await ctx.RespondAsync($"Successfully unbanned **{userToUnban.Username}#{userToUnban.Discriminator}**!");
        }
    }
}

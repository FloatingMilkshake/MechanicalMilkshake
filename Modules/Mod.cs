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
        [Description("Speak through the bot!")]
        [RequireUserPermissions(Permissions.KickMembers)]
        public async Task Tellraw(CommandContext ctx, [Description("The channel to send the message in.")] DiscordChannel targetChannel, [Description("The message to have the bot send."), RemainingText] string message)
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
                await ctx.RespondAsync($"I sent your message to {targetChannel.Mention}.");
            }
        }

        [Command("clear")]
        [Aliases("purge")]
        [Description("Deletes a given number of messages from a channel.")]
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
        [Description("Kicks a user. They can rejoin the server if they have an invite.")]
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
            await ctx.Channel.SendMessageAsync($"**{userToKick.Username}#{userToKick.Discriminator}** has been kicked: **{reason}**");
        }

        [Command("ban")]
        [Aliases("bonk")]
        [Description("Bans a user. They will not be able to rejoin unless unbanned.")]
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
            await ctx.Channel.SendMessageAsync($"**{userToBan.Username}#{userToBan.Discriminator}** has been banned: **{reason}**");
        }

        [Command("unban")]
        [Description("Unbans a user.")]
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

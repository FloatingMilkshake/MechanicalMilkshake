using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class Mod : BaseCommandModule
    {
        [Command("clear")]
        [Aliases("purge")]
        [Description("Deletes a given number of messages from a channel.")]
        [RequirePermissions(Permissions.ManageMessages)]
        public async Task Clear(CommandContext ctx, [Description("The number of messages to delete.")] int count)
        {
            await ctx.Message.DeleteAsync();
            var messages = await ctx.Channel.GetMessagesAsync(count);
            await ctx.Channel.DeleteMessagesAsync(messages);
            var response = await ctx.Channel.SendMessageAsync($"Deleted {messages.Count} messages!");
            await Task.Delay(3000);
            await response.DeleteAsync();
        }

        [Command("kick")]
        [Aliases("yeet")]
        [Description("Kicks a user. They can rejoin the server if they have an invite.")]
        [RequirePermissions(Permissions.KickMembers)]
        public async Task Kick(CommandContext ctx, [Description("The user to kick.")] DiscordUser userToKick, [Description("The reason for the kick."), RemainingText] string reason)
        {
            DiscordMember memberToKick = default;
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
        public async Task Ban(CommandContext ctx, [Description("The user to ban.")] DiscordUser userToBan, [Description("The reason for the ban."), RemainingText] string reason)
        {
            try
            {
                await ctx.Guild.BanMemberAsync(userToBan.Id, 0, reason);
            }
            catch
            {
                await ctx.RespondAsync($"Something went wrong. You or I may not be allowed to ban **{userToBan.Username}#{userToBan.Discriminator}**! Please check the role hierarchy and permissions.");
                return;
            }
            await ctx.Message.DeleteAsync();
            await ctx.Channel.SendMessageAsync($"**{userToBan.Username}#{userToBan.Discriminator}** has been banned: **{reason}**");
        }

        [Command("delete")]
        [Description("Delete a message. This can be used to to delete direct messages with the bot where you are normally unable to delete its messages.")]
        [Aliases("deletemsg", "delmsg")]
        public async Task Delete(CommandContext ctx, DiscordMessage message)
        {
            try
            {
                await ctx.Channel.DeleteMessageAsync(message);
                var successMsg = await ctx.RespondAsync("Message deleted successfully.");
                await Task.Delay(3000);
                await ctx.Channel.DeleteMessageAsync(successMsg);
            }
            catch (Exception e)
            {
                var failureMsg = await ctx.RespondAsync($"Something went wrong! See details below.\n\n```\n{e}\n```\n(This message will be automatically deleted in 15 seconds.)");
                await Task.Delay(15000);
                await ctx.Channel.DeleteMessageAsync(failureMsg);
            }
        }
    }
}

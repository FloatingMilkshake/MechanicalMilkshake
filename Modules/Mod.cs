using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.SlashCommands.Attributes;
using System;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
    public class Mod : ApplicationCommandModule
    {
        [SlashCommand("tellraw", "Speak through the bot! Requires that you have either the Kick Members or Ban Members permission.")]
        public async Task Tellraw(InteractionContext ctx, [Option("message", "The message to have the bot send.")] string message, [Option("channel", "The channel to send the message in.")] DiscordChannel channel = null)
        {
            if (!ctx.Member.Permissions.HasPermission(Permissions.KickMembers) && !ctx.Member.Permissions.HasPermission(Permissions.BanMembers))
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
            System.Collections.Generic.IReadOnlyList<DiscordMessage> messages = await ctx.Channel.GetMessagesAsync((int)count);
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

        [SlashCommand("nickname", "Changes my nickname.")]
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
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Nickname changed to **{nickname}** successfully!").AsEphemeral(true));
            }
            else
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Nickname cleared successfully!").AsEphemeral(true));
            }
        }
    }
}

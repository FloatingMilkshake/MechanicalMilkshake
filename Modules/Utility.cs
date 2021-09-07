using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.IO;

namespace DiscordBot.Commands
{
    public class Utility : BaseCommandModule
    {

        [Command("clear")]
        [Aliases("purge", "delete")]
        [Description("Deletes the given number of messages from a channel.")]
        public async Task Clear(CommandContext ctx, int count)
        {
            try
            {
                await ctx.Message.DeleteAsync();
                var messages = await ctx.Channel.GetMessagesAsync(count);
                await ctx.Channel.DeleteMessagesAsync(messages);
                var response = await ctx.Channel.SendMessageAsync($"Deleted {count} messages!");
                await Task.Delay(3000);
                await response.DeleteAsync();
            }
            catch (Exception e)
            {
                await ctx.RespondAsync($"A problem occurred while trying to execute that command!\n\n```\n{e}\n```");
            }
        }

        [Command("userinfo")]
        [Description("Returns information about the provided user.")]
        public async Task UserInfo(CommandContext ctx, DiscordMember member = null)
        {
            if (member == null)
            {
                member = ctx.Member;
            }

            //await ctx.RespondAsync($"**User Info for {user.Username}#{user.Discriminator}**"
            //    + "*more coming soon when i actually write the rest of this command*");

            var embed = new DiscordEmbedBuilder()
                .WithDescription($"**User Info for {member.Mention}**")
                .WithColor(new DiscordColor($"{member.Color}"))
                .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                .AddField("additional field", "aaaaaaaaaaa", false)
                .AddField($"another additional field: {member.Id}", $"description: {member.Id}")
                .AddField($"field with no description: {member.Id}", "**aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa**");

            await ctx.RespondAsync(embed);
        }

        [Command("serverinfo")]
        [Description("Returns information about the server.")]
        public async Task ServerInfo(CommandContext ctx)
        {
            await ctx.RespondAsync($"**Server Info for {ctx.Guild.Name}**\n*more info coming soon when i decide to improve this command*");
        }

        [Command("avatar")]
        [Description("Returns the avatar of the provided user.")]
        public async Task Avatar(CommandContext ctx, DiscordMember user)
        {
            if (user is null)
            {
                user = (DiscordMember)ctx.Message.Author;
            }

            await ctx.RespondAsync(user.AvatarUrl);
        }
    }
}
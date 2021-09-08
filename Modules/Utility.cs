using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Modules
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
        [Aliases("whois")]
        [Description("Returns information about the provided user.")]
        public async Task UserInfo(CommandContext ctx, DiscordMember member = null)
        {
            if (member == null)
            {
                member = ctx.Member;
            }

            var msSinceEpoch = member.Id >> 22;
            var msUnix = msSinceEpoch + 1420070400000;
            var registeredAt = ($"{(msUnix / 1000).ToString()}");

            TimeSpan t = member.JoinedAt - new DateTime(1970, 1, 1);
            int joinedAtTimestamp = (int)t.TotalSeconds;

            String memberRoles = null;
            foreach (var role in member.Roles)
            {
                memberRoles += " " + role.ToString();
                memberRoles = memberRoles.Replace("Role ", "<@&");
                Regex pattern = new Regex(@";.*");
                Match match = pattern.Match(memberRoles);
                String stringToReplace = match.ToString();
                memberRoles = memberRoles.Replace($"{stringToReplace}", ">");
            }

            String acknowledgements = "None";
            if (member.Permissions.HasPermission(Permissions.KickMembers) && member.Permissions.HasPermission(Permissions.BanMembers))
            {
                acknowledgements = "Server Moderator (can kick and ban members)";
            }
            if (member.Permissions.HasPermission(Permissions.Administrator))
            {
                acknowledgements = "Server Administrator";
            }
            if (member.IsOwner)
            {
                acknowledgements = "Server Owner";
            }

            var embed = new DiscordEmbedBuilder()
                .WithAuthor($"User Info for {member.Username}#{member.Discriminator}")
                .WithDescription($"{member.Mention}")
                .WithColor(new DiscordColor($"{member.Color}"))
                .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                .AddField("ID", $"{member.Id}")
                .AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)")
                .AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)")
                .AddField("Roles", $"{memberRoles}")
                .AddField("Acknowledgements", $"{acknowledgements}")
                .WithThumbnail(member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow);

            await ctx.RespondAsync(embed);
        }

        [Command("serverinfo")]
        [Description("Returns information about the server.")]
        public async Task ServerInfo(CommandContext ctx)
        {
            await ctx.RespondAsync($"**Server Info for {ctx.Guild.Name}**\n*more info coming soon when i decide to improve this command*");
        }

        [Command("avatar")]
        [Aliases("avy", "av")]
        [Description("Returns the avatar of the provided user.")]
        public async Task Avatar(CommandContext ctx, DiscordMember member = null)
        {
            if (member == null)
            {
                member = ctx.Member;
            }

            await ctx.RespondAsync(member.AvatarUrl);
        }

        [Command("timestamp")]
        [Aliases("ts")]
        [Description("Returns the Unix timestamp of a given Discord ID/snowflake")]
        public async Task TimestampUnixCmd(CommandContext ctx, [Description("The ID/snowflake to fetch the Unix timestamp for")] ulong snowflake)
        {
            var msSinceEpoch = snowflake >> 22;
            var msUnix = msSinceEpoch + 1420070400000;
            await ctx.RespondAsync($"{(msUnix / 1000).ToString()}");
        }
    }
}
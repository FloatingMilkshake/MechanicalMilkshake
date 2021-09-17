using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DiscordBot.Modules
{
    public class Utility : BaseCommandModule
    {
        [Command("userinfo")]
        [Aliases("whois")]
        [Description("Returns information about the provided user.")]
        public async Task UserInfo(CommandContext ctx, [Description("The member to look up information for. Defaults to yourself if no member is provided.")] DiscordMember member = null)
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

            await ctx.RespondAsync($"User Info for **{member.Username}#{member.Discriminator}**", embed);
        }

        [Command("serverinfo")]
        [Description("Returns information about the server.")]
        public async Task ServerInfo(CommandContext ctx)
        {
            var description = "None";

            if (ctx.Guild.Description is not null)
            {
                description = ctx.Guild.Description;
            }

            var msSinceEpoch = ctx.Guild.Id >> 22;
            var msUnix = msSinceEpoch + 1420070400000;
            var createdAt = $"{(msUnix / 1000).ToString()}";

            var embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor("#9B59B6"))
                .AddField("Server Owner", $"{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}", true)
                .AddField("Channels", $"{ctx.Guild.Channels.Count}", true)
                .AddField("Members", $"{ctx.Guild.MemberCount}", true)
                .AddField("Roles", $"{ctx.Guild.Roles.Count}", true)
                .WithThumbnail($"{ctx.Guild.IconUrl}")
                .AddField("Description", $"{description}", true)
                .WithFooter($"Server ID: {ctx.Guild.Id}")
                .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)", true);

            await ctx.RespondAsync($"Server Info for **{ctx.Guild.Name}**", embed);
        }

        [Command("avatar")]
        [Aliases("avy", "av")]
        [Description("Returns the avatar of the provided user.")]
        public async Task Avatar(CommandContext ctx, [Description("The member to get the avatar for. Defaults to yourself if no member is provided.")] DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.Message.Author;
            }

            await ctx.RespondAsync(user.AvatarUrl);
        }

        [Group("timestamp")]
        [Aliases("ts")]
        [Description("Returns the Unix timestamp of a given date.")]
        class TimestampCmds : BaseCommandModule
        {
            [GroupCommand]
            [Description("Returns the Unix timestamp of a given Discord ID/snowflake.")]
            public async Task TimestampSnowflakeCmd(CommandContext ctx, [Description("The ID/snowflake to fetch the Unix timestamp for.")] ulong snowflake)
            {
                var msSinceEpoch = snowflake >> 22;
                var msUnix = msSinceEpoch + 1420070400000;
                await ctx.RespondAsync($"{(msUnix / 1000).ToString()}");
            }

            [Command("date")]
            [Aliases("string", "d")]
            [Description("Returns the Unix timestamp of a given date.")]
            public async Task TimestampDateCmd(CommandContext ctx, [Description("The date to fetch the Unix timestamp for."), RemainingText] string date)
            {
                DateTime dateToConvert = Convert.ToDateTime(date);
                await ctx.RespondAsync($"{dateToConvert.ToString()}");
                long unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();
                await ctx.RespondAsync($"{unixTime.ToString()}");
            }
        }

        [Command("lookup")]
        [Description("Look up a user not in the current server.")]
        public async Task Lookup(CommandContext ctx, [Description("The user you want to look up.")] DiscordUser user)
        {
            var msSinceEpoch = user.Id >> 22;
            var msUnix = msSinceEpoch + 1420070400000;
            var createdAt = ($"{(msUnix / 1000).ToString()}");

            var embed = new DiscordEmbedBuilder()
                .WithThumbnail($"{user.AvatarUrl}")
                .AddField("ID", $"{user.Id}")
                .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

            await ctx.RespondAsync($"Information about **{user.Username}#{user.Discriminator}**:", embed);
        }

        [Command("markdown")]
        [Description("Expose the Markdown formatting behind a message!")]
        [Aliases("md", "raw")]
        public async Task Markdown(CommandContext ctx, [Description("The message you want to expose the formatting of. Accepts message IDs (for messages in the same channel) and links.")] DiscordMessage message)
        {
            var msgContentEscaped = message.Content.Replace("`", @"\`");
            msgContentEscaped = msgContentEscaped.Replace("*", @"\*");
            msgContentEscaped = msgContentEscaped.Replace("_", @"\_");
            msgContentEscaped = msgContentEscaped.Replace("~", @"\~");
            msgContentEscaped = msgContentEscaped.Replace(">", @"\>");
            await ctx.RespondAsync($"{msgContentEscaped}");
        }

        [Command("ping")]
        [Description("Checks my ping.")]
        public async Task Ping(CommandContext ctx)
        {
            var msg = await ctx.RespondAsync("Pong!");
            ulong ping = (msg.Id - ctx.Message.Id) >> 22;
            await msg.ModifyAsync($"Pong! `{ping}ms`");
        }

        [Command("wolframalpha")]
        [Description("Search WolframAlpha without leaving Discord!")]
        [Aliases("wa", "wolfram")]
        public async Task WolframAlpha(CommandContext ctx, [Description("What to search for."), RemainingText] string query)
        {
            var msg = await ctx.RespondAsync("Searching...");

            string appid;
            if (Environment.GetEnvironmentVariable("WOLFRAMALPHA_APP_ID") == "yourappid") {
                await msg.ModifyAsync("Looks like you don't have an App ID! Check the `WOLFRAMALPHA_APP_ID` environment variable. "
                    + "If you don't know how to get an App ID, see Getting Started here: <https://products.wolframalpha.com/short-answers-api/documentation/>");
                return;
            }
            else
            {
                appid = Environment.GetEnvironmentVariable("WOLFRAMALPHA_APP_ID");
            }
            
            try
            {
                var cli = new WebClient();
                string data = cli.DownloadString($"https://api.wolframalpha.com/v1/result?appid={appid}&i={query}");
                await msg.ModifyAsync(data);
            }
            catch
            {
                await msg.ModifyAsync("Something went wrong while searching WolframAlpha! There may not be a simple answer to your query.");
            }
        }
    }
}

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace MechanicalMilkshake.Modules
{
    public class Utility : ApplicationCommandModule
    {
        [SlashCommand("userinfo", "Returns information about the provided user.")]
        public async Task UserInfo(InteractionContext ctx, [Option("member", "The member to look up information for. Defaults to yourself if no member is provided.")] DiscordUser user = null)
        {
            DiscordMember member = null;

            if (user != null)
            {
                try
                {
                    member = await ctx.Guild.GetMemberAsync(user.Id);
                }
                catch
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Hmm. It doesn't look like that user is in the server, so I can't pull up their user info."));
                    return;
                }
            }
            if (member == null)
            {
                member = ctx.Member;
            }

            ulong msSinceEpoch = member.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string registeredAt = ($"{msUnix / 1000}");

            TimeSpan t = member.JoinedAt - new DateTime(1970, 1, 1);
            int joinedAtTimestamp = (int)t.TotalSeconds;

            string acknowledgements = null;
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

            string roles = "None";
            if (member.Roles.Any())
            {
                roles = "";
                foreach (DiscordRole role in member.Roles.OrderBy(role => role.Position).Reverse())
                {
                    roles += role.Mention + " ";
                }
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor($"{member.Color}"))
                .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                .AddField("User Mention", member.Mention)
                .AddField("User ID", $"{member.Id}")
                .AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)")
                .AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)")
                .AddField("Roles", roles)
                .WithThumbnail(member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow);

            if (acknowledgements != null)
            {
                embed.AddField("Acknowledgements", acknowledgements);
            }

            if (member.PremiumSince != null)
            {
                DateTime PremiumSinceUtc = member.PremiumSince.Value.UtcDateTime;
                long unixTime = ((DateTimeOffset)PremiumSinceUtc).ToUnixTimeSeconds();
                string boostingSince = $"Boosting since <t:{unixTime}:R> (<t:{unixTime}:F>";

                embed.AddField("Server Booster", boostingSince);
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"User Info for **{member.Username}#{member.Discriminator}**").AddEmbed(embed));
        }

        [SlashCommand("serverinfo", "Returns information about the server.")]
        public async Task ServerInfo(InteractionContext ctx)
        {
            string description = "None";

            if (ctx.Guild.Description is not null)
            {
                description = ctx.Guild.Description;
            }

            ulong msSinceEpoch = ctx.Guild.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string createdAt = $"{msUnix / 1000}";

            DiscordMember botUserAsMember = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithColor(new DiscordColor($"{botUserAsMember.Color}"))
                .AddField("Server Owner", $"{ctx.Guild.Owner.Username}#{ctx.Guild.Owner.Discriminator}", true)
                .AddField("Channels", $"{ctx.Guild.Channels.Count}", true)
                .AddField("Members", $"{ctx.Guild.MemberCount}", true)
                .AddField("Roles", $"{ctx.Guild.Roles.Count}", true)
                .WithThumbnail($"{ctx.Guild.IconUrl}")
                .AddField("Description", $"{description}", true)
                .WithFooter($"Server ID: {ctx.Guild.Id}")
                .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)", true);

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Server Info for **{ctx.Guild.Name}**").AddEmbed(embed));
        }

        [SlashCommand("avatar", "Returns the avatar of the provided user. Defaults to yourself if no user is provided.")]
        public async Task Avatar(InteractionContext ctx, [Option("member", "The server member to get the avatar for.")] DiscordUser user = null)
        {
            if (user == null)
            {
                user = ctx.User;
            }

            string avatarLink = $"{user.AvatarUrl}".Replace("size=1024", "size=4096");

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(avatarLink));
        }

        [SlashCommandGroup("timestamp", "Returns the Unix timestamp of a given date.")]
        class TimestampCmds : ApplicationCommandModule
        {
            [SlashCommand("id", "Returns the Unix timestamp of a given Discord ID/snowflake.")]
            public async Task TimestampSnowflakeCmd(InteractionContext ctx, [Option("snowflake", "The ID/snowflake to fetch the Unix timestamp for.")] string id, [Option("format", "The format to convert the timestamp to. Options are F/D/T/R/f/d/t.")] string format = null)
            {
                ulong snowflake = default;

                try
                {
                    snowflake = Convert.ToUInt64(id);
                }
                catch
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Hmm, that doesn't look like a valid ID/snowflake. I wasn't able to convert it to a timestamp."));
                    return;
                }

                ulong msSinceEpoch = snowflake >> 22;
                ulong msUnix = msSinceEpoch + 1420070400000;
                if (format == null)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{msUnix / 1000}"));
                }
                else
                {
                    string[] validFormats = { "F", "f", "T", "t", "D", "d", "R" };
                    if (validFormats.Any(format.Contains))
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"<t:{msUnix / 1000}:{format}>"));
                    }
                    else
                    {
                        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Hmm, that doesn't look like a valid format. Here's the raw Unix timestamp: {msUnix / 1000}"));
                    }
                }
            }

            [SlashCommand("date", "Returns the Unix timestamp of a given date.")]
            public async Task TimestampDateCmd(InteractionContext ctx, [Option("date", "The date to fetch the Unix timestamp for.")] string date)
            {
                DateTime dateToConvert = Convert.ToDateTime(date);
                long unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{unixTime}"));
            }
        }

        [SlashCommand("lookup", "Look up a user not in the current server.")]
        public async Task Lookup(InteractionContext ctx, [Option("user", "The user you want to look up.")] DiscordUser user)
        {
            ulong msSinceEpoch = user.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string createdAt = ($"{msUnix / 1000}");

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithThumbnail($"{user.AvatarUrl}")
                .AddField("ID", $"{user.Id}")
                .AddField("Account created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)");

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Information about **{user.Username}#{user.Discriminator}**:").AddEmbed(embed));
        }

        [SlashCommand("markdown", "Expose the Markdown formatting behind a message!")]
        public async Task Markdown(InteractionContext ctx, [Option("message", "The message you want to expose the formatting of. Accepts message IDs.")] string messageToExpose)
        {
            ulong messageId;
            try
            {
                messageId = Convert.ToUInt64(messageToExpose);
            }
            catch
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Hmm, that doesn't look like a valid message ID. I wasn't able to get the Markdown data from it."));
                return;
            }

            DiscordMessage message = await ctx.Channel.GetMessageAsync(messageId);

            string msgContentEscaped = message.Content.Replace("`", @"\`");
            msgContentEscaped = msgContentEscaped.Replace("*", @"\*");
            msgContentEscaped = msgContentEscaped.Replace("_", @"\_");
            msgContentEscaped = msgContentEscaped.Replace("~", @"\~");
            msgContentEscaped = msgContentEscaped.Replace(">", @"\>");
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"{msgContentEscaped}"));
        }

        [SlashCommand("ping", "Checks my ping.")]
        public async Task Ping(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Pong! `{ctx.Client.Ping}ms`"));
        }

        [SlashCommand("wolframalpha", "Search WolframAlpha without leaving Discord!")]
        public async Task WolframAlpha(InteractionContext ctx, [Option("query", "What to search for.")] string query)
        {
            string queryEncoded;
            if (query == null)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Hmm, it doesn't look like you entered a valid query. Try something like `/wolframalpha query:What is the meaning of life?`."));
                return;
            }
            else
            {
                queryEncoded = HttpUtility.UrlEncode(query);
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Searching..."));

            string appid;
            if (Program.configjson.WolframAlphaAppId == null)
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Looks like you don't have an App ID! Check the wolframAlphaAppId field in your config.json file. "
                    + "If you don't know how to get an App ID, see Getting Started here: <https://products.wolframalpha.com/short-answers-api/documentation/>"));
                return;
            }
            else
            {
                appid = Program.configjson.WolframAlphaAppId;
            }

            try
            {
                string data = await Program.httpClient.GetStringAsync($"https://api.wolframalpha.com/v1/result?appid={appid}&i={query}");
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent(data + $"\n\n*Query URL: <https://www.wolframalpha.com/input/?i={queryEncoded}>*"));
            }
            catch
            {
                await ctx.EditResponseAsync(new DiscordWebhookBuilder().WithContent("Something went wrong while searching WolframAlpha and I couldn't get a simple answer for your query! Note that I cannot return all data however, and a result may be available here: "
                    + $"<https://www.wolframalpha.com/input/?i={queryEncoded}>"));
            }
        }

        [SlashCommand("charactercount", "Counts the characters in a message.")]
        public async Task CharacterCount(InteractionContext ctx, [Option("message", "The message to count the characters of.")] string chars)
        {
            int count = 0;
            foreach (char chr in chars)
            {
                count++;
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(count.ToString()));
        }
    }
}

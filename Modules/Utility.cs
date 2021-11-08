using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
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

            ulong msSinceEpoch = member.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string registeredAt = ($"{msUnix / 1000}");

            TimeSpan t = member.JoinedAt - new DateTime(1970, 1, 1);
            int joinedAtTimestamp = (int)t.TotalSeconds;

            String memberRoles = null;
            foreach (DiscordRole role in member.Roles)
            {
                memberRoles += " " + role.ToString();
                memberRoles = memberRoles.Replace("Role ", "<@&");
                Regex pattern = new(@";.*");
                Match match = pattern.Match(memberRoles);
                String stringToReplace = match.ToString();
                memberRoles = memberRoles.Replace($"{stringToReplace}", ">");
            }

            String acknowledgements = null;
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

            string roles;
            if (memberRoles == null)
            {
                roles = "None";
            }
            else
            {
                roles = memberRoles;
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithDescription($"{member.Mention}")
                .WithColor(new DiscordColor($"{member.Color}"))
                .WithFooter($"Requested by {ctx.Member.Username}#{ctx.Member.Discriminator}")
                .AddField("ID", $"{member.Id}")
                .AddField("Account registered on", $"<t:{registeredAt}:F> (<t:{registeredAt}:R>)")
                .AddField("Joined server on", $"<t:{joinedAtTimestamp}:F> (<t:{joinedAtTimestamp}:R>)")
                .AddField("Roles", roles)
                .WithThumbnail(member.AvatarUrl)
                .WithTimestamp(DateTime.UtcNow);

            if (acknowledgements != null)
            {
                embed.AddField("Acknowledgements", acknowledgements);
            }

            await ctx.RespondAsync($"User Info for **{member.Username}#{member.Discriminator}**", embed);
        }

        [Command("serverinfo")]
        [Description("Returns information about the server.")]
        public async Task ServerInfo(CommandContext ctx)
        {
            string description = "None";

            if (ctx.Guild.Description is not null)
            {
                description = ctx.Guild.Description;
            }

            ulong msSinceEpoch = ctx.Guild.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string createdAt = $"{msUnix / 1000}";

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
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
        [Description("Returns the avatar of the provided user. Defaults to yourself if no user is provided.")]
        public async Task Avatar(CommandContext ctx, [Description("The server member to get the avatar for."), RemainingText] DiscordMember member = null)
        {
            if (member == null)
            {
                member = await ctx.Guild.GetMemberAsync(ctx.Message.Author.Id);
            }

            string avatarLink = $"{member.AvatarUrl}".Replace("size=1024", "size=4096");

            await ctx.RespondAsync(avatarLink);
        }

        [Command("avatar")]
        public async Task Avatar(CommandContext ctx, [Description("The user to get the avatar for."), RemainingText] DiscordUser user = null)
        {
            if (user == null)
            {
                // other command above will take care of this
                return;
            }
            else
            {
                string avatarLink = $"{user.AvatarUrl}".Replace("size=1024", "size=4096");

                await ctx.RespondAsync(avatarLink);
            }
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
                ulong msSinceEpoch = snowflake >> 22;
                ulong msUnix = msSinceEpoch + 1420070400000;
                await ctx.RespondAsync($"{msUnix / 1000}");
            }

            [Command("date")]
            [Aliases("string", "d")]
            [Description("Returns the Unix timestamp of a given date.")]
            public async Task TimestampDateCmd(CommandContext ctx, [Description("The date to fetch the Unix timestamp for."), RemainingText] string date)
            {
                DateTime dateToConvert = Convert.ToDateTime(date);
                long unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();
                await ctx.RespondAsync($"{unixTime}");
            }
        }

        [Command("lookup")]
        [Description("Look up a user not in the current server.")]
        public async Task Lookup(CommandContext ctx, [Description("The user you want to look up.")] DiscordUser user)
        {
            ulong msSinceEpoch = user.Id >> 22;
            ulong msUnix = msSinceEpoch + 1420070400000;
            string createdAt = ($"{msUnix / 1000}");

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
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
            string msgContentEscaped = message.Content.Replace("`", @"\`");
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
            DiscordMessage msg = await ctx.RespondAsync("Pong!");
            ulong responseTime = (msg.Id - ctx.Message.Id) >> 22;
            await msg.ModifyAsync($"Pong! `{ctx.Client.Ping}ms`"
                + $"\nIt took me `{responseTime}ms` to respond to your message.");
        }

        [Command("wolframalpha")]
        [Description("Search WolframAlpha without leaving Discord!")]
        [Aliases("wa", "wolfram")]
        public async Task WolframAlpha(CommandContext ctx, [Description("What to search for."), RemainingText] string query)
        {
            if (query == null)
            {
                await ctx.RespondAsync("Hmm, it doesn't look like you entered a valid query. Try something like `~wolframalpha What is the meaning of life?`.");
                return;
            }

            DiscordMessage msg = await ctx.RespondAsync("Searching...");

            string appid;
            if (Environment.GetEnvironmentVariable("WOLFRAMALPHA_APP_ID") == "yourappid")
            {
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
                WebClient cli = new WebClient();
                string data = cli.DownloadString($"https://api.wolframalpha.com/v1/result?appid={appid}&i={query}");
                await msg.ModifyAsync(data);
            }
            catch
            {
                try
                {
                    WebClient cli = new WebClient();
                    string data = cli.DownloadString($"https://api.wolframalpha.com/v1/simple?appid={appid}&i={query}");
                    await msg.ModifyAsync($"Something went wrong while searching WolframAlpha and I couldn't get a simple answer for your query. Here's a more detailed result: {data}");
                }
                catch
                {
                    await msg.ModifyAsync("Something went wrong while searching WolframAlpha! There may not be an answer to your query.");
                }
            }
        }

        [Command("charactercount")]
        [Description("Counts the characters in a message.")]
        [Aliases("charcount", "count", "chars")]
        public async Task CharacterCount(CommandContext ctx, [RemainingText] string chars)
        {
            int count = 0;
            foreach (char chr in chars)
            {
                count++;
            }

            await ctx.RespondAsync(count.ToString());
        }

        [Command("deletemessage")]
        [Description("Delete a message. This can be used to to delete direct messages with the bot where you are normally unable to delete its messages.")]
        [Aliases("deletemsg", "delmsg")]
        public async Task Delete(CommandContext ctx, string message)
        {
            DiscordMember author;
            if (message == "all")
            {
                if (!ctx.Channel.IsPrivate)
                {
                    await ctx.RespondAsync($"`{ctx.Command} all` can only be run in Direct Messages!");
                    return;
                }

                DiscordMessage response = await ctx.RespondAsync("This might take a while. Working...");

                System.Collections.ObjectModel.Collection<DiscordMessage> messagesToDelete = new() { };
                System.Collections.Generic.IReadOnlyList<DiscordMessage> messagesToConsider = await ctx.Channel.GetMessagesAsync(100);
                foreach (DiscordMessage msg in messagesToConsider)
                {
                    if (msg.Author == ctx.Client.CurrentUser && msg != response)
                    {
                        messagesToDelete.Add(msg);
                    }
                }

                if (messagesToDelete.Count == 0)
                {
                    await response.ModifyAsync("Something went wrong!\n" +
                        "Looks like none of the last 100 messages were sent by me!\n" +
                        "(This message will be automatically deleted in 15 seconds.)");
                    await Task.Delay(15000);
                    try
                    {
                        await ctx.Channel.DeleteMessageAsync(response);
                    }
                    catch
                    {
                        // just silencing the exception here because this will probably only fail if the error msg is deleted with the delete cmd before the 15 seconds are up
                    }
                }
                else
                {
                    foreach (DiscordMessage msg in messagesToDelete)
                    {
                        try
                        {
                            await ctx.Channel.DeleteMessageAsync(msg);
                            await Task.Delay(3000);
                        }
                        catch (Exception e)
                        {
                            await response.ModifyAsync("Something went wrong!\n" +
                                $"```\n{e}\n```");
                        }
                    }
                }

                await response.ModifyAsync("Done! This message will be deleted automatically in 10 seconds.");
                await Task.Delay(15000);
                try
                {
                    await ctx.Channel.DeleteMessageAsync(response);
                }
                catch
                {
                    // just silencing the exception here because this will probably only fail if the error msg is deleted with the delete cmd before the 10 seconds are up
                }
            }
            else
            {
                if (!ctx.Channel.IsPrivate)
                {
                    author = await ctx.Guild.GetMemberAsync(ctx.Message.Author.Id);
                    if (!author.Permissions.HasPermission(Permissions.ManageMessages))
                    {
                        await ctx.RespondAsync("You don't have permission to use this command here!\n`delete` requires the Manage Messages permission when being used in a non-DM channel.");
                        return;
                    }
                }

                try
                {
                    DiscordMessage msg = null;
                    try
                    {
                        msg = await ctx.Channel.GetMessageAsync(Convert.ToUInt64(message));
                    }
                    catch
                    {
                        await ctx.RespondAsync($"That doesn't look like a message ID! Make sure you've got the right thing. A message ID will look something like this: `{ctx.Message.Id}`");
                        return;
                    }
                    await ctx.Channel.DeleteMessageAsync(msg);
                    DiscordMessage successMsg = await ctx.RespondAsync("Message deleted successfully.");
                    await Task.Delay(3000);
                    await ctx.Channel.DeleteMessageAsync(successMsg);
                }
                catch (DSharpPlus.Exceptions.NotFoundException)
                {
                    DiscordMessage failureMsg = await ctx.RespondAsync("Something went wrong!\n" +
                        "The message you're trying to delete cannot be found. Note that you cannot delete messages in one server from another, or from DMs.\n" +
                        "(This message will be automatically deleted in 15 seconds.)");
                    await Task.Delay(15000);
                    try
                    {
                        await ctx.Channel.DeleteMessageAsync(failureMsg);
                    }
                    catch
                    {
                        // just silencing the exception here because this will probably only fail if the error msg is deleted with the delete cmd before the 15 seconds are up
                    }
                }
                catch (DSharpPlus.Exceptions.UnauthorizedException)
                {
                    DiscordMessage failureMsg = await ctx.RespondAsync("Something went wrong!\n" +
                        "I don't have permission to delete that message.\n" +
                        "(This message will be automatically deleted in 15 seconds.)");
                    await Task.Delay(15000);
                    try
                    {
                        await ctx.Channel.DeleteMessageAsync(failureMsg);
                    }
                    catch
                    {
                        // silencing exception
                    }
                }
                catch (Exception e)
                {
                    DiscordMessage failureMsg = await ctx.RespondAsync($"Something went wrong! See details below.\n\n```\n{e}\n```\n(This message will be automatically deleted in 15 seconds.)");
                    await Task.Delay(15000);
                    try
                    {
                        await ctx.Channel.DeleteMessageAsync(failureMsg);
                    }
                    catch
                    {
                        // silencing exception
                    }
                }
            }
        }
    }
}

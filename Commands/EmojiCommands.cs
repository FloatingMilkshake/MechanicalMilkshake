namespace MechanicalMilkshake.Commands
{
    public class EmojiCommands : ApplicationCommandModule
    {
        [SlashCommand("stealemoji",
            "Fetch all of a server's emoji! Note that the bot must be in the server for this to work.")]
        public async Task StealEmoji(InteractionContext ctx,
            [Option("server", "The ID of the server to fetch emoji from.")]
        string server,
            [Option("add_to_server", "Whether to add all of the emoji to the server you're running the command in.")]
        bool addToServer = false)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            ulong guildId;
            DiscordGuild guild;
            try
            {
                guildId = Convert.ToUInt64(server);
                guild = await ctx.Client.GetGuildAsync(guildId);
            }
            catch (UnauthorizedException)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I was able to find that server, but I don't have access to its emoji! The most likely reason for this is that I am not in that server; I cannot fetch emoji from a server I am not in. If you think I am in the server and you're still seeing this, contact the bot owner for help."));
                return;
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"I couldn't find that server! Make sure `{server}` is a server ID. If you're sure it is and you're still seeing this, contact the bot owner for help."));
                return;
            }

            var response = $"Emoji for **{guild.Name}**\n\n";

            var staticEmoji = "";
            var animatedEmoji = "";

            var copySuccess = false;
            var copyFail = false;
            var dm = false;

            foreach (var emoji in guild.Emojis)
            {
                if (emoji.Value.IsAnimated)
                    animatedEmoji += $" <a:{emoji.Value.Name}:{emoji.Key}>";
                else
                    staticEmoji += $" <:{emoji.Value.Name}:{emoji.Key}>";

                if (addToServer)
                {
                    if (ctx.Channel.IsPrivate)
                    {
                        dm = true;
                        continue;
                    }

                    if (!ctx.Member.Permissions.HasPermission(Permissions.ManageEmojis) ||
                        !ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.ManageEmojis))
                        copyFail = true;

                    var stream = await Program.httpClient.GetStreamAsync(emoji.Value.Url);
                    using (MemoryStream ms = new())
                    {
                        stream.CopyTo(ms);
                        ms.Position = 0;
                        await ctx.Guild.CreateEmojiAsync(emoji.Value.Name, ms, null,
                            $"Emoji copied from {guild.Id} with /{ctx.CommandName} by {ctx.User.Id}.");
                    }

                    copySuccess = true;
                }
            }

            if (staticEmoji != "") response += $"Static Emoji:\n{staticEmoji}\n\n";

            if (animatedEmoji != "") response += $"Animated Emoji:\n{animatedEmoji}\n\n";

            if (copySuccess) response += "These emoji have been copied to this server.";

            if (copyFail)
                response +=
                    "I couldn't copy these emoji to this server! Make sure you and I both have the \"Manage Emojis and Stickers\" permission.";

            if (dm)
                response +=
                    "I can't add emoji when you use this command in DMs! Please use it in the server you want to add the emoji to.";

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
        }

        [SlashCommand("bigemoji", "Enlarge an emoji! Only works for custom emoji.")]
        public async Task BigEmoji(InteractionContext ctx, [Option("emoji", "The emoji to enlarge.")] string emoji)
        {
            Regex emojiRegex = new(@"<(a)?:.*:([0-9]*)>");

            var matches = emojiRegex.Matches(emoji);
            var groups = matches[0].Groups;

            if (groups[1].Value == "a")
                await ctx.CreateResponseAsync(
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"https://cdn.discordapp.com/emojis/{groups[2].Value}.gif"));
            else
                await ctx.CreateResponseAsync(
                    new DiscordInteractionResponseBuilder().WithContent(
                        $"https://cdn.discordapp.com/emojis/{groups[2].Value}"));
        }
    }
}

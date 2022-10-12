namespace MechanicalMilkshake.Commands;

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

        foreach (var character in server)
            if (!char.IsNumber(character))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"That doesn't look like a server ID!\nA server ID is composed of only numbers and looks something like this: `{ctx.Guild.Id}`.\nPlease try again."));
                return;
            }

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
                "I was able to find that server, but I don't have access to its emoji! The most likely reason for this is that I am not in that server; I cannot fetch emoji from a server I am not in." +
                "\n\nIf you think I am in the server and you're still seeing this, contact the bot owner for help (if you don't know who that is, see `/about`!)."));
            return;
        }
        catch
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"I couldn't find that server! Make sure `{server}` is a server ID. If you're sure it is and you're still seeing this, contact the bot owner for help."));
            return;
        }

        var response = $"Emoji for **{guild.Name}**\n\n";

        List<DiscordEmoji> stolenEmoji = new();
        List<DiscordEmoji> copiedEmoji = new();
        List<DiscordEmoji> failedEmoji = new();

        foreach (var emoji in guild.Emojis)
        {
            stolenEmoji.Add(emoji.Value);

            if (addToServer)
            {
                if (ctx.Channel.IsPrivate)
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "I can't add emoji when you use this command in DMs! Please use it in the server you want to add the emoji to."));
                    return;
                }

                if (!ctx.Member.Permissions.HasPermission(Permissions.ManageEmojis) ||
                    !ctx.Guild.CurrentMember.Permissions.HasPermission(Permissions.ManageEmojis))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "I couldn't copy these emoji to this server! Make sure you and I both have the \"Manage Emojis and Stickers\" permission."));
                    return;
                }

                var stream = await Program.httpClient.GetStreamAsync(emoji.Value.Url);
                using MemoryStream ms = new();
                stream.CopyTo(ms);
                ms.Position = 0;
                try
                {
                    await ctx.Guild.CreateEmojiAsync(emoji.Value.Name, ms, null,
                        $"Emoji copied from guild {guild.Id} with /{ctx.CommandName} by user {ctx.User.Username}#{ctx.User.Discriminator} ({ctx.User.Id}).");
                }
                catch
                {
                    failedEmoji.Add(emoji.Value);
                    continue;
                }

                copiedEmoji.Add(emoji.Value);
            }
        }

        // Static emoji
        response += "**Static Emoji**\n";
        foreach (var emoji in stolenEmoji)
            if (!emoji.IsAnimated)
                response += $"<:{emoji.Name}:{emoji.Id}> ";

        // Animated emoji
        response += "\n\n**Animated Emoji**\n";
        foreach (var emoji in stolenEmoji)
            if (emoji.IsAnimated)
                response += $"<a:{emoji.Name}:{emoji.Id}> ";

        if (addToServer && failedEmoji.Count > 0)
        {
            // Emoji were copied, but some failed.

            response += "\n\nThe following emoji were **not** copied to this server due to an unknown error:\n";
            foreach (var emoji in failedEmoji)
                response += emoji.IsAnimated ? $"<a:{emoji.Name}:{emoji.Id}> " : $"<:{emoji.Name}:{emoji.Id}> ";
        }
        else if (copiedEmoji.Count > 0)
        {
            response += "\n\nThese emoji were copied to this server.";
        }

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }

    [SlashCommand("bigemoji", "Enlarge an emoji! Only works for custom emoji.")]
    public async Task BigEmoji(InteractionContext ctx, [Option("emoji", "The emoji to enlarge.")] string emoji)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        Regex emojiRegex = new(@"<(a)?:.*:([0-9]*)>");

        if (!emojiRegex.IsMatch(emoji))
        {
            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                    "That doesn't look like an emoji! Please try again."));
            return;
        }

        var matches = emojiRegex.Matches(emoji);
        var groups = matches[0].Groups;

        string emojiUrl;
        if (groups[1].Value == "a")
            emojiUrl = $"https://cdn.discordapp.com/emojis/{groups[2].Value}.gif";
        else
            emojiUrl = $"https://cdn.discordapp.com/emojis/{groups[2].Value}";

        string response;

        HttpRequestMessage httpRequest = new(HttpMethod.Get, emojiUrl);
        var httpResponse = await Program.httpClient.SendAsync(httpRequest);
        if (httpResponse.IsSuccessStatusCode)
            response = emojiUrl;
        else
            response = "That emoji doesn't exist! Please try again.";

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }
}
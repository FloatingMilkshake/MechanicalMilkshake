namespace MechanicalMilkshake.Commands;

[Command("emoji")]
[Description("Commands for working with emoji.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
[InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
public partial class Emoji
{
    private static readonly Regex EmojiRegex = EmojiPattern();
    
    [Command("get")]
    [Description("Get all emoji from a server. I must be in the server for this to work.")]
    public static async Task GetEmoji(SlashCommandContext ctx,
        [Parameter("server"), Description("The ID of the server to get emoji from. I must be in the server for this to work!")] string server,
        [Parameter("zip"), Description("Whether to include a zip file containing all of the emoji. Defaults to True.")] bool zip = true)
    {
        // Defer interaction response
        await ctx.DeferResponseAsync();

        // Validate guild ID
        if (server.Any(character => !char.IsNumber(character)))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "That doesn't look like a server ID!\n"
                + "A server ID is composed of only numbers and looks something like this: `799644062973427743`.\n"
                + "Please try again."));
            return;
        }

        // Fetch guild
        DiscordGuild guild;
        try
        {
            var guildId = Convert.ToUInt64(server);
            guild = await ctx.Client.GetGuildAsync(guildId);
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "I was able to find that server, but I don't have access to its emoji! "
                + "The most likely reason for this is that I am not in that server; "
                + "I cannot fetch emoji from a server I am not in."
                + "\n\nIf you think I am in the server and you're still seeing this, contact a bot owner for "
                + $"help (if you don't know who that is, see {SlashCmdMentionHelpers.GetSlashCmdMention("about")}!)."));
            return;
        }
        catch
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"I couldn't find that server! That either means I'm not in it, or {(server.Length > 20 ? "that" : $"`{server}`")} isn't a server ID. "
                + "If you've tried again and you're still seeing this, contact a bot owner for help."));
            return;
        }

        // Keep a list of found emoji for downloading
        var foundEmoji = guild.Emojis.Values.ToList();

        // Collect emoji and create response

        var response = $"Emoji for **{guild.Name}**\n\n";

        // Static emoji
        response += "**Static Emoji**\n";
        response = foundEmoji.Where(emoji => !emoji.IsAnimated)
            .Aggregate(response, (current, emoji) => current + $"<:{emoji.Name}:{emoji.Id}> ");

        // Animated emoji
        response += "\n\n**Animated Emoji**\n";
        response = foundEmoji.Where(emoji => emoji.IsAnimated)
            .Aggregate(response, (current, emoji) => current + $"<a:{emoji.Name}:{emoji.Id}> ");

        // Check response length to ensure the length limit is not hit
        if (response.Length > 2000)
        {
            response = "Hmm, it looks like there are too many emoji in that server to show them all here! A zip file is attached containing all of them.";
            zip = true;
        }

        // Create response builder; will be updated to add zip file if necessary, or sent as-is
        var responseBuilder = new DiscordFollowupMessageBuilder().WithContent(response);

        // Fetch each emoji, download & archive
        var tempDir = "";
        FileStream file = default;
        if (zip)
        {
            // Create a temporary directory to store the emoji
            tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);

            var downloadCompleteSuccess = true; // this is bad prove true pls

            // Download each emoji
            foreach (var emoji in foundEmoji)
            {
                var emojiUrl = emoji.IsAnimated
                    ? $"https://cdn.discordapp.com/emojis/{emoji.Id}.gif?size=4096"
                    : $"https://cdn.discordapp.com/emojis/{emoji.Id}.png?size=4096";

                HttpRequestMessage httpRequest = new(HttpMethod.Get, emojiUrl);
                var httpResponse = await Program.HttpClient.SendAsync(httpRequest);
                if (!httpResponse.IsSuccessStatusCode)
                {
                    downloadCompleteSuccess = false;
                    continue;
                }

                var emojiPath = Path.Combine(tempDir, $"{emoji.Name}." + (emoji.IsAnimated ? "gif" : "png"));
                await using var emojiStream = await httpResponse.Content.ReadAsStreamAsync();
                await using var fileStream = File.Create(emojiPath);
                await emojiStream.CopyToAsync(fileStream);
            }

            var zipPath = Path.Combine(tempDir, $"{guild.Id}.zip");

            // Add each emoji to zip
            await using (var zipStream = File.Create(zipPath))
            {
                ZipArchive zipArchive = new(zipStream, ZipArchiveMode.Create);
                foreach (var emoji in foundEmoji)
                {
                    var emojiPath = Path.Combine(tempDir, $"{emoji.Name}." + (emoji.IsAnimated ? "gif" : "png"));
                    zipArchive.CreateEntryFromFile(emojiPath, $"{emoji.Name}." + (emoji.IsAnimated ? "gif" : "png"));
                }
            }

            // Add zip & note to response
            response += "\n\nA zip file is attached containing all of the emoji from this server.";

            file = File.OpenRead(zipPath);
            responseBuilder.AddFile($"{guild.Id}.zip", file);

            // Include note if some emoji failed to download
            if (!downloadCompleteSuccess)
            {
                response += "\n**Note:** Some emoji failed to download and are not included in the zip file.";
            }

            // Update response in builder
            responseBuilder.WithContent(response);
        }

        await ctx.FollowupAsync(responseBuilder);

        // Clean up
        if (file is not null) await file.DisposeAsync();
        if (!string.IsNullOrWhiteSpace(tempDir))
            Directory.Delete(tempDir, true);
    }

    [Command("enlarge")]
    [Description("Enlarge an emoji! Only works for custom emoji.")]
    public static async Task EnlargeEmoji(SlashCommandContext ctx, [Parameter("emoji"), Description("The emoji to enlarge.")] string emoji)
    {
        await ctx.DeferResponseAsync();

        if (!EmojiRegex.IsMatch(emoji))
        {
            await ctx.FollowupAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                    "That doesn't look like an emoji! Please try again."));
            return;
        }

        var matches = EmojiRegex.Matches(emoji);

        var response = "";
        foreach (var match in matches.Cast<Match>())
        {
            var groups = match.Groups;
            var emojiUrl = groups[1].Value == "a"
            ? $"https://cdn.discordapp.com/emojis/{groups[3].Value}.gif"
            : $"https://cdn.discordapp.com/emojis/{groups[3].Value}.png";

            HttpRequestMessage httpRequest = new(HttpMethod.Get, emojiUrl);
            var httpResponse = await Program.HttpClient.SendAsync(httpRequest);
            response += (httpResponse.IsSuccessStatusCode ? emojiUrl : "[This emoji doesn't seem to exist! Please try again...]") + "\n";
        }

        if (response.Length > 2000)
            response = "It looks like this message is too long to send! Try entering less emoji, or contact a bot owner if you need help.";

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }

    [GeneratedRegex("<(a)?:([A-Za-z0-9_]*):([0-9]*)>")]
    private static partial Regex EmojiPattern();
}

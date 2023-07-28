﻿using System.IO.Compression;

namespace MechanicalMilkshake.Commands;

public class EmojiCommands : ApplicationCommandModule
{
    private static readonly Regex EmojiRegex = new(@"<(a)?:([A-z]*):([0-9]*)>");

    [SlashCommandGroup("emoji", "Commands for working with emoji.")]
    [SlashCommandPermissions(Permissions.ManageEmojis)]
    public class Emoji
    {
        [SlashCommand("get", "Get all emoji from a server. The bot must be in the server for this to work.")]
        public static async Task GetEmoji(InteractionContext ctx,
            [Option("server", "The ID of the server to get emoji from.")] string server,
            [Option("zip", "Whether to include a zip file containing all of the emoji. Defaults to True.")] bool zip = true)
        {
            // Defer interaction response
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            // Validate guild ID
            if (server.Any(character => !char.IsNumber(character)))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
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
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I was able to find that server, but I don't have access to its emoji! "
                    + "The most likely reason for this is that I am not in that server; "
                    + "I cannot fetch emoji from a server I am not in."
                    + "\n\nIf you think I am in the server and you're still seeing this, contact the bot owner for "
                    + $"help (if you don't know who that is, see {SlashCmdMentionHelpers.GetSlashCmdMention("about")}!)."));
                return;
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"I couldn't find that server! Make sure `{server}` is a server ID. "
                    + "If you're sure it is and you're still seeing this, contact the bot owner for help."));
                return;
            }

            // Keep a list of found emoji for downloading
            var foundEmoji = new List<DiscordEmoji>();
            foreach (DiscordEmoji emoji in guild.Emojis.Values)
            {
                foundEmoji.Add(emoji);
            }

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

            // Create response builder; will be updated to add zip file if necessary, or sent as-is
            var responseBuilder = new DiscordFollowupMessageBuilder().WithContent(response);

            // Fetch each emoji, download & archive
            string tempDir = "";
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
                using (FileStream zipStream = File.Create(zipPath))
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

            await ctx.FollowUpAsync(responseBuilder);

            // Clean up
            await file.DisposeAsync();
            Directory.Delete(tempDir, true);
        }

        [SlashCommand("enlarge", "Enlarge an emoji! Only works for custom emoji.")]
        public static async Task EnlargeEmoji(InteractionContext ctx, [Option("emoji", "The emoji to enlarge.")] string emoji)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!EmojiRegex.IsMatch(emoji))
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "That doesn't look like an emoji! Please try again."));
                return;
            }

            var matches = EmojiRegex.Matches(emoji);

            var response = "";
            foreach (Match match in matches)
            {
                var groups = match.Groups;
                var emojiUrl = groups[1].Value == "a"
                ? $"https://cdn.discordapp.com/emojis/{groups[3].Value}.gif"
                : $"https://cdn.discordapp.com/emojis/{groups[3].Value}.png";

                HttpRequestMessage httpRequest = new(HttpMethod.Get, emojiUrl);
                var httpResponse = await Program.HttpClient.SendAsync(httpRequest);
                response += (httpResponse.IsSuccessStatusCode ? emojiUrl : "[This emoji doesn't seem to exist! Please try again...]") + "\n";
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
        }
    }
}
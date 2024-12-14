namespace MechanicalMilkshake.Commands;

public partial class Markdown
{
    [Command("markdown")]
    [Description("Expose the Markdown formatting behind a message!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)] // TODO: make a context menu command that works everywhere
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild)]
    public static async Task MarkdownCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("message"), Description("The message you want to expose the formatting of. Accepts message IDs and links.")]
        string messageToExpose)
    {
        await ctx.DeferResponseAsync();

        DiscordMessage message;
        if (DiscordUrlPattern().IsMatch(messageToExpose))
        {
            // Assume the user provided a message link. Extract channel and message IDs to get message content.
            
            // Pattern to extract channel and message IDs from URL
            var idPattern = IdPattern();

            // Get channel ID
            var targetChannelId =
                Convert.ToUInt64(idPattern.Match(messageToExpose).Groups[1].ToString().Replace("/", ""));

            // Try to fetch channel
            DiscordChannel channel;
            try
            {
                channel = await ctx.Client.GetChannelAsync(targetChannelId);
            }
            catch
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I wasn't able to find that message! Make sure I have permission to see the channel it's in."));
                return;
            }
            
            // Get message ID
            var targetMessage =
                Convert.ToUInt64(idPattern.Match(messageToExpose).Groups[2].ToString().Replace("/", ""));

            // Try to fetch message
            try
            {
                message = await channel.GetMessageAsync(targetMessage);
            }
            catch
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I wasn't able to read that message! Make sure I have permission to access it."));
                return;
            }
        }
        else
        {
            if (messageToExpose.Length < 17)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Hmm, that doesn't look like a valid message ID or link."));
                return;
            }

            ulong messageId;
            try
            {
                messageId = Convert.ToUInt64(messageToExpose);
            }
            catch
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Hmm, that doesn't look like a valid message ID or link. I wasn't able to get the Markdown data from it."));
                return;
            }

            try
            {
                message = await ctx.Channel.GetMessageAsync(messageId);
            }
            catch
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I wasn't able to read that message! Make sure I have permission to access it."));
                return;
            }
        }

        var markdown = message.Content;
        var embeds = message.Embeds;

        var response = new DiscordFollowupMessageBuilder()
            .WithContent($"Here's the Markdown data for [that message]({message.JumpLink}):");

        if (!string.IsNullOrWhiteSpace(markdown))
            response.AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Message Content")
                .WithDescription(MarkdownHelpers.Parse(markdown))
                .WithColor(Program.BotColor));

        if (embeds.Count > 0)
            foreach (var embed in embeds)
            {
                if (response.Embeds.Count >= 10)
                    continue;
                if (embed.Type == "image")
                    continue;
                if (string.IsNullOrWhiteSpace(embed.Title) && string.IsNullOrWhiteSpace(embed.Description) &&
                    string.IsNullOrWhiteSpace(embed.Footer?.Text) && string.IsNullOrWhiteSpace(embed.Author?.Name) &&
                    string.IsNullOrWhiteSpace(embed.Fields.ToString()))
                    continue;

                var markdownEmbed = new DiscordEmbedBuilder()
                    .WithTitle(string.IsNullOrWhiteSpace(embed.Title)
                        ? "Embed Content"
                        : $"Embed Content: {MarkdownHelpers.Parse(embed.Title)}")
                    .WithDescription(embeds[0].Description is not null ? MarkdownHelpers.Parse(embed.Description) : "")
                    .WithColor(embed.Color.HasValue == false ? Program.BotColor : embed.Color.Value);

                if (embed.Fields is not null)
                    foreach (var field in embed.Fields)
                        markdownEmbed.AddField(MarkdownHelpers.Parse(field.Name), MarkdownHelpers.Parse(field.Value),
                            field.Inline);
                response.AddEmbed(markdownEmbed);
            }

        if (response.Embeds.Count == 0)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "That message doesn't have any text content! I can only parse the Markdown data from messages with content."));
            return;
        }

        // If the embeds have more than 6000 characters, return a kind message instead of a 400 error.
        if (response.Embeds.Sum(e =>
                e.Description.Length + e.Title.Length + e.Fields.Sum(f => f.Name.Length + f.Value.Length)) > 6000)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "That message is too long! I can only parse the Markdown data from messages shorter than 6000 characters."));
            return;
        }

        await ctx.FollowupAsync(response);
    }

    [GeneratedRegex(@".*.discord.com\/channels\/([\d+]*\/)+[\d+]*")]
    private static partial Regex DiscordUrlPattern();
    [GeneratedRegex("[A-z]")]
    private static partial Regex AlphanumericCharacterPatern();
    [GeneratedRegex(@"(?:.*\/)([0-9]{1,})\/([0-9]{1,})$")]
    private static partial Regex IdPattern();
}
namespace MechanicalMilkshake.Commands;

internal class MarkdownCommands
{
    [Command("Show Raw Content")]
    [Description("Show the raw Markdown formatting behind a message.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public static async Task MarkdownMessageContextMenuCommandAsync(MessageCommandContext ctx, DiscordMessage targetMessage)
    {
        await ctx.DeferResponseAsync(true);
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder(GetMarkdownDataForMessage(targetMessage)).AsEphemeral(true));
    }

    [Command("markdown")]
    [Description("Expose the Markdown formatting behind a message!")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
    public static async Task MarkdownCommandAsync(SlashCommandContext ctx,
        [Parameter("message"), Description("The message you want to expose the formatting of. Accepts message IDs and links.")]
        string messageIdOrLink)
    {
        await ctx.DeferResponseAsync();
        await ctx.FollowupAsync(await GetMarkdownDataForMessageAsync(messageIdOrLink, ctx.Channel));
    }

    private static async Task<DiscordMessageBuilder> GetMarkdownDataForMessageAsync(string messageIdOrLink, DiscordChannel currentChannel)
    {
        var response = new DiscordMessageBuilder();

        DiscordMessage message = default;
        var urlPatternMatch = Setup.Constants.RegularExpressions.DiscordUrlPattern.Match(messageIdOrLink);
        if (urlPatternMatch.Success)
        {
            DiscordChannel channel = default;
            try
            {
                if (urlPatternMatch.Groups[2].Value == currentChannel.Id.ToString())
                    channel = currentChannel;
                else
                    channel = await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(urlPatternMatch.Groups[2].Value));
                message = await channel.GetMessageAsync(Convert.ToUInt64(urlPatternMatch.Groups[3].Value));
            }
            catch
            {
                if (channel != default && channel.Id == currentChannel.Id)
                    response.WithContent("I wasn't able to read that message! Make sure I have permission to read message history.");
                else
                    response.WithContent("I wasn't able to find that message! Make sure I have permission to see the channel it's in.");
            }
        }
        else
        {
            var idPatternMatch = Setup.Constants.RegularExpressions.DiscordIdPattern.Match(messageIdOrLink);
            if (!idPatternMatch.Success)
            {
                response.WithContent("That doesn't look like a valid message ID or link! Please try again.");
            }
            else
            {
                try
                {
                    message = await currentChannel.GetMessageAsync(Convert.ToUInt64(idPatternMatch.Value));
                }
                catch (UnauthorizedException)
                {
                    response.WithContent("I wasn't able to read that message! Make sure I have permission to read message history.");
                }
                catch (NotFoundException)
                {
                    response.WithContent("I couldn't find that message! If it's in another channel, you'll need to provide a message link.");
                }
            }
        }

        if (message != default)
            response = GetMarkdownDataForMessage(message);

        return response;
    }

    private static DiscordMessageBuilder GetMarkdownDataForMessage(DiscordMessage message)
    {
        var response = new DiscordMessageBuilder();

        if (!string.IsNullOrWhiteSpace(message.Content))
            response.AddEmbed(new DiscordEmbedBuilder()
                .WithTitle("Message Content")
                .WithDescription(EscapeString(message.Content))
                .WithColor(Setup.Constants.BotColor));

        foreach (var embed in message.Embeds)
        {
            if (response.Embeds.Count >= 10 || embed.Type != "rich" ||
                (string.IsNullOrWhiteSpace(embed.Title) && string.IsNullOrWhiteSpace(embed.Description) &&
                string.IsNullOrWhiteSpace(embed.Footer?.Text) && string.IsNullOrWhiteSpace(embed.Author?.Name) &&
                string.IsNullOrWhiteSpace(embed.Fields.ToString())))
            {
                continue;
            }

            var markdownEmbed = new DiscordEmbedBuilder()
                .WithTitle(string.IsNullOrWhiteSpace(embed.Title)
                    ? "Embed Content"
                    : $"Embed Content: {EscapeString(embed.Title)}")
                .WithDescription(message.Embeds[0].Description is not null ? EscapeString(embed.Description) : "")
                .WithColor(embed.Color.HasValue == false ? default : embed.Color.Value);

            if (embed.Fields is not null)
                foreach (var field in embed.Fields)
                    markdownEmbed.AddField(EscapeString(field.Name), EscapeString(field.Value), field.Inline);

            response.AddEmbed(markdownEmbed);
        }

        if (response.Embeds.Count == 0)
        {
            response.WithContent("That message doesn't have any text content! I can only parse the Markdown data from messages with content.");
        }
        else if (response.Embeds.Sum(e => e.Description.Length + e.Title.Length + e.Fields.Sum(f => f.Name.Length + f.Value.Length)) > 6000)
        {
            response.WithContent("Sorry, the output is too long for me to send here!");
        }
        else
        {
            response.WithContent($"Here's the Markdown data for [that message]({message.JumpLink}):");
        }

        return response;
    }

    private static string EscapeString(string input)
    {
        var output = input
            .Replace(@"\", @"\\")
            .Replace("`", @"\`")
            .Replace("*", @"\*")
            .Replace("_", @"\_")
            .Replace("~", @"\~")
            .Replace(">", @"\>")
            .Replace("[", @"\[")
            .Replace("]", @"\]")
            .Replace("(", @"\(")
            .Replace(")", @"\)")
            .Replace("#", @"\#")
            .Replace("|", @"\|");

        return output.Length > 4000
            ? "Sorry, the output is too long for me to send here!"
            : output;
    }
}

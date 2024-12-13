namespace MechanicalMilkshake.Commands;

[RequireGuild]
public partial class Clear
{
    public static readonly Dictionary<ulong, List<DiscordMessage>> MessagesToClear = new();

    [Command("clear")]
    [Description("Delete many messages from the current channel.")]
    [RequirePermissions(DiscordPermission.ManageMessages)]
    public static async Task ClearCommand(SlashCommandContext ctx,
        [Parameter("count"), Description("The number of messages to consider for deletion. Required if you don't use the 'up_to' argument.")]
        long count = 0,
        [Parameter("up_to"), Description("Optionally delete messages up to (not including) this one. Accepts IDs and links.")]
        string upTo = "",
        [Parameter("user"), Description("Optionally filter the deletion to a specific user.")]
        DiscordUser user = default,
        [Parameter("ignore_me"), Description("Optionally filter the deletion to only messages not sent by you.")]
        bool ignoreMe = false,
        [Parameter("match"), Description("Optionally filter the deletion to only messages containing certain text.")]
        string match = "",
        [Parameter("include_embeds"), Description("Optionally include embed content when searching for matches.")]
        bool includeEmbeds = false,
        [Parameter("bots_only"), Description("Optionally filter the deletion to only bots.")]
        bool botsOnly = false,
        [Parameter("humans_only"), Description("Optionally filter the deletion to only humans.")]
        bool humansOnly = false,
        [Parameter("attachments_only"), Description("Optionally filter the deletion to only messages with attachments.")]
        bool attachmentsOnly = false,
        [Parameter("links_only"), Description("Optionally filter the deletion to only messages containing links.")]
        bool linksOnly = false,
        [Parameter("dry_run"), Description("Optionally show the number of messages that would be deleted, without actually deleting them.")]
        bool dryRun = false
    )
    {
        await ctx.DeferResponseAsync(true);

        var discordLinkRx = DiscordLinkPattern();

        // Credit to @Erisa for this line of regex. https://github.com/Erisa/Cliptok/blob/a80e700/Constants/RegexConstants.cs#L8
        var urlRx = UrlPattern();

        switch (count)
        {
            // If all args are unset
            case 0 when upTo == "" && user == default && ignoreMe == false && match == "" && includeEmbeds == false &&
                        botsOnly == false && humansOnly == false && attachmentsOnly == false && linksOnly == false &&
                        dryRun == false:
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You must provide at least one argument! I need to know which messages to delete.")
                    .AsEphemeral());
                return;
            case 0 when upTo == "":
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "I need to know how many messages to delete! Please provide a value for `count` or `up_to`.")
                    .AsEphemeral());
                return;
            // If count is too low or too high, refuse the request
            case < 0:
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "I can't delete a negative number of messages! Try setting `count` to a positive number.")
                    .AsEphemeral());
                return;
            case >= 1000:
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "Deleting that many messages poses a risk of something disastrous happening, so I'm refusing your request, sorry.")
                    .AsEphemeral());
                return;
        }

        // Get messages to delete, whether that's messages up to a certain one or the last 'x' number of messages.

        if (upTo != "" && count != 0)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    "You can't provide both a count of messages and a message to delete up to! Please only provide one of the two arguments.")
                .AsEphemeral());
            return;
        }

        List<DiscordMessage> messagesToClear = [];
        if (upTo == "")
        {
            var messages = await ctx.Channel.GetMessagesAsync((int)count).ToListAsync();
            messagesToClear = messages.ToList();
        }
        else
        {
            ulong messageId;
            if (!upTo.Contains("discord.com"))
            {
                if (!ulong.TryParse(upTo, out messageId))
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "That doesn't look like a valid message ID or link! Please try again."));
                    return;
                }
            }
            else
            {
                if (
                    discordLinkRx.Match(upTo).Groups[2].Value != ctx.Channel.Id.ToString()
                    || !ulong.TryParse(discordLinkRx.Match(upTo).Groups[3].Value, out messageId)
                )
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Please provide a valid link to a message in this channel!")
                        .AsEphemeral());
                    return;
                }
            }

            // This is the message we will delete up to. This message will not be deleted.
            var message = await ctx.Channel.GetMessageAsync(messageId);

            // List of messages to delete, up to (not including) the one we just got.
            var firstMsg = (await ctx.Channel.GetMessagesAfterAsync(message.Id, 1).ToListAsync())[0];
            var firstMsgId = firstMsg.Id;
            messagesToClear.Add(firstMsg);
            while (true)
            {
                var newMessages = await ctx.Channel.GetMessagesAfterAsync(firstMsgId).ToListAsync();
                messagesToClear.AddRange(newMessages);
                firstMsgId = newMessages.First().Id;
                if (newMessages.Count < 100)
                    break;
            }
        }

        // Now we know how many messages we'll be looking through and we won't be refusing the request. Time to check filters.
        // Order of priority here is the order of the arguments for the command.

        // Match user
        if (user != default)
            foreach (var message in messagesToClear.ToList().Where(message => message.Author.Id != user.Id))
                messagesToClear.Remove(message);

        // Ignore me
        if (ignoreMe)
            foreach (var message in messagesToClear.ToList().Where(message => message.Author == ctx.User))
                messagesToClear.Remove(message);
        
        // Match text, including embeds if selected
        if (match != "")
        {
            foreach (var message in messagesToClear.ToList()
                .Where(message => !message.Content.Contains(match, StringComparison.OrdinalIgnoreCase)))
            {
                if (includeEmbeds && message.Embeds.Count > 0)
                {
                    var embeds = message.Embeds.ToList();
                    foreach (var _ in embeds.Where(embed => embed.Description is not null && !embed.Description.Contains(match, StringComparison.OrdinalIgnoreCase)
                                 && embed.Fields.ToList().All(field => !field.Value.Contains(match, StringComparison.OrdinalIgnoreCase))))
                    {
                        messagesToClear.Remove(message);
                    }
                }
                else messagesToClear.Remove(message);
            }
        }

        // Bots only
        if (botsOnly)
        {
            if (humansOnly)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "You can't use `bots_only` and `humans_only` together! Pick one or the other please.")
                    .AsEphemeral());
                return;
            }

            foreach (var message in messagesToClear.ToList().Where(message => !message.Author.IsBot))
                messagesToClear.Remove(message);
        }

        // Humans only
        if (humansOnly)
            foreach (var message in messagesToClear.ToList().Where(message => message.Author.IsBot))
                messagesToClear.Remove(message);

        // Attachments only
        if (attachmentsOnly)
        {
            if (linksOnly)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "You can't use `images_only` and `links_only` together! Pick one or the other please.")
                    .AsEphemeral());
                return;
            }

            foreach (var message in messagesToClear.ToList().Where(message => message.Attachments.Count == 0))
                messagesToClear.Remove(message);
        }

        // Links only
        if (linksOnly)
            foreach (var message in messagesToClear.ToList()
                         .Where(message => !urlRx.IsMatch(message.Content.ToLower())))
                messagesToClear.Remove(message);

        // Skip messages older than 2 weeks, since Discord won't let us delete them anyway

        var skipped = false;
        foreach (var message in messagesToClear.ToList().Where(message =>
                     message.CreationTimestamp.ToUniversalTime() < DateTime.UtcNow.AddDays(-14)))
        {
            messagesToClear.Remove(message);
            skipped = true;
        }

        switch (messagesToClear.Count)
        {
            case 0 when skipped:
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("All of the messages to delete are older than 2 weeks, so I can't delete them!")
                    .AsEphemeral());
                return;
            // All filters checked. 'messages' is now our final list of messages to delete.
            // Warn if we're going to be deleting 50 or more messages.
            case >= 50:
            {
                DiscordButtonComponent confirmButton =
                    new(DiscordButtonStyle.Danger, "clear-confirm-callback", "Delete Messages", disabled: dryRun);
                var confirmationMessage = await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(dryRun ? $"You would be about to delete {messagesToClear.Count} messages, but since "
                                            + "you used `dry_run = True`, I won't do anything."
                                        : $"You're about to delete {messagesToClear.Count} messages. Are you sure?")
                    .AddComponents(confirmButton).AsEphemeral());

                MessagesToClear.Add(confirmationMessage.Id, messagesToClear);
                break;
            }
            case >= 1:
            {
                if (dryRun)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent($"You would be about to"
                    + $" delete {messagesToClear.Count} messages, but since you used `dry_run = True`,"
                    + " I won't do anything.").AsEphemeral());
                    break;
                }

                try
                {
                    await ctx.Channel.DeleteMessagesAsync(messagesToClear,
                        $"[Clear by {UserInfoHelpers.GetFullUsername(ctx.User)}]");
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("I don't have permission to delete messages in this channel! Make sure I have the `Manage Messages` permission.")
                        .AsEphemeral());
                    return;
                }
                // not catching other exceptions because they are handled by generic slash error handler, but this one deserves a clear message
                
                if (skipped)
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"Cleared **{messagesToClear.Count}** messages from {ctx.Channel.Mention}!\nSome messages were not deleted because they are older than 2 weeks.")
                        .AsEphemeral());
                else
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"Cleared **{messagesToClear.Count}** messages from {ctx.Channel.Mention}!")
                        .AsEphemeral());
                break;
            }
            default:
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "There were no messages that matched all of the arguments you provided! Nothing to do."));
                break;
        }
    }

    [GeneratedRegex(@".*discord(?:app)?.com\/channels\/((?:@)?[a-z0-9]*)\/([0-9]*)(?:\/)?([0-9]*)")]
    private static partial Regex DiscordLinkPattern();
    [GeneratedRegex("(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]")]
    private static partial Regex UrlPattern();
}
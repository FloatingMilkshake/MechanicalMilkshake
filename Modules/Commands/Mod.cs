using DSharpPlus.Exceptions;

namespace MechanicalMilkshake.Modules.Commands;

[SlashRequireGuild]
public class Mod : ApplicationCommandModule
{
    public static Dictionary<ulong, List<DiscordMessage>> MessagesToClear = new();

    [SlashCommand("clear", "Delete many messages from the current channel.", false)]
    [SlashCommandPermissions(Permissions.ManageMessages)]
    public async Task Clear(InteractionContext ctx,
        [Option("count",
            "The number of messages to consider for deletion. Required if you don't use the 'upto' argument.")]
        long count = 0,
        [Option("up_to", "Optionally delete messages up to (not including) this one. Accepts IDs and links.")]
        string upTo = "",
        [Option("user", "Optionally filter the deletion to a specific user.")]
        DiscordUser user = default,
        [Option("ignore_me", "Optionally filter the deletion to only messages not sent by you.")]
        bool ignoreMe = false,
        [Option("match", "Optionally filter the deletion to only messages containing certain text.")]
        string match = "",
        [Option("bots_only", "Optionally filter the deletion to only bots.")]
        bool botsOnly = false,
        [Option("humans_only", "Optionally filter the deletion to only humans.")]
        bool humansOnly = false,
        [Option("attachments_only", "Optionally filter the deletion to only messages with attachments.")]
        bool attachmentsOnly = false,
        [Option("links_only", "Optionally filter the deletion to only messages containing links.")]
        bool linksOnly = false
    )
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral());

        Regex discord_link_rx = new(@".*discord(?:app)?.com\/channels\/((?:@)?[a-z0-9]*)\/([0-9]*)(?:\/)?([0-9]*)");

        // Credit to @Erisa for this line of regex. https://github.com/Erisa/Cliptok/blob/a80e700/Constants/RegexConstants.cs#L8
        Regex url_rx = new("(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z0-9][a-z0-9-]{0,61}[a-z0-9]");

        // If all args are unset
        if (count == 0 && upTo == "" && user == default && ignoreMe == false && match == "" && botsOnly == false &&
            humansOnly == false && attachmentsOnly == false && linksOnly == false)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("You must provide at least one argument! I need to know which messages to delete.")
                .AsEphemeral());
            return;
        }

        if (count == 0 && upTo == "")
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    "I need to know how many messages to delete! Please provide a value for `count` or `up_to`.")
                .AsEphemeral());
            return;
        }

        // If count is too low or too high, refuse the request

        if (count < 0)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    "I can't delete a negative number of messages! Try setting `count` to a positive number.")
                .AsEphemeral());
            return;
        }

        if (count >= 1000)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    "Deleting that many messages poses a risk of something disastrous happening, so I'm refusing your request, sorry.")
                .AsEphemeral());
            return;
        }

        // Get messages to delete, whether that's messages up to a certain one or the last 'x' number of messages.

        if (upTo != "" && count != 0)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    "You can't provide both a count of messages and a message to delete up to! Please only provide one of the two arguments.")
                .AsEphemeral());
            return;
        }

        List<DiscordMessage> messagesToClear;
        if (upTo == "")
        {
            var messages = await ctx.Channel.GetMessagesAsync((int)count);
            messagesToClear = messages.ToList();
        }
        else
        {
            DiscordMessage message;
            ulong messageId;
            if (!upTo.Contains("discord.com"))
            {
                if (!ulong.TryParse(upTo, out messageId))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "That doesn't look like a valid message ID or link! Please try again."));
                    return;
                }
            }
            else
            {
                if (
                    discord_link_rx.Match(upTo).Groups[2].Value != ctx.Channel.Id.ToString()
                    || !ulong.TryParse(discord_link_rx.Match(upTo).Groups[3].Value, out messageId)
                )
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Please provide a valid link to a message in this channel!")
                        .AsEphemeral());
                    return;
                }
            }

            // This is the message we will delete up to. This message will not be deleted.
            message = await ctx.Channel.GetMessageAsync(messageId);

            // List of messages to delete, up to (not including) the one we just got.
            var messages = await ctx.Channel.GetMessagesAfterAsync(message.Id);
            messagesToClear = messages.ToList();
        }

        // Now we know how many messages we'll be looking through and we won't be refusing the request. Time to check filters.
        // Order of priority here is the order of the arguments for the command.

        // Match user
        if (user != default)
            foreach (var message in messagesToClear.ToList())
                if (message.Author.Id != user.Id)
                    messagesToClear.Remove(message);

        // Ignore me
        if (ignoreMe)
            foreach (var message in messagesToClear.ToList())
                if (message.Author == ctx.User)
                    messagesToClear.Remove(message);

        // Match text
        if (match != "")
            foreach (var message in messagesToClear.ToList())
                if (!message.Content.ToLower().Contains(match.ToLower()))
                    messagesToClear.Remove(message);

        // Bots only
        if (botsOnly)
        {
            if (humansOnly)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "You can't use `bots_only` and `humans_only` together! Pick one or the other please.")
                    .AsEphemeral());
                return;
            }

            foreach (var message in messagesToClear.ToList())
                if (!message.Author.IsBot)
                    messagesToClear.Remove(message);
        }

        // Humans only
        if (humansOnly)
            foreach (var message in messagesToClear.ToList())
                if (message.Author.IsBot)
                    messagesToClear.Remove(message);

        // Attachments only
        if (attachmentsOnly)
        {
            if (linksOnly)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "You can't use `images_only` and `links_only` together! Pick one or the other please.")
                    .AsEphemeral());
                return;
            }

            foreach (var message in messagesToClear.ToList())
                if (message.Attachments.Count == 0)
                    messagesToClear.Remove(message);
        }

        // Links only
        if (linksOnly)
            foreach (var message in messagesToClear.ToList())
                if (!url_rx.IsMatch(message.Content.ToLower()))
                    messagesToClear.Remove(message);

        // Skip messages older than 2 weeks, since Discord won't let us delete them anyway

        var skipped = false;
        foreach (var message in messagesToClear.ToList())
            if (message.CreationTimestamp.ToUniversalTime() < DateTime.UtcNow.AddDays(-14))
            {
                messagesToClear.Remove(message);
                skipped = true;
            }

        if (messagesToClear.Count == 0 && skipped)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("All of the messages to delete are older than 2 weeks, so I can't delete them!")
                .AsEphemeral());
            return;
        }

        // All filters checked. 'messages' is now our final list of messages to delete.

        // Warn the mod if we're going to be deleting 50 or more messages.
        if (messagesToClear.Count >= 50)
        {
            DiscordButtonComponent confirmButton =
                new(ButtonStyle.Danger, "clear-confirm-callback", "Delete Messages");
            var confirmationMessage = await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("messages. Are you sure?").AddComponents(confirmButton).AsEphemeral());

            MessagesToClear.Add(confirmationMessage.Id, messagesToClear);
        }
        else
        {
            if (messagesToClear.Count >= 1)
            {
                await ctx.Channel.DeleteMessagesAsync(messagesToClear,
                    $"[Clear by {ctx.User.Username}#{ctx.User.Discriminator}]");
                if (skipped)
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(
                            $"Cleared **{messagesToClear.Count}** messages from {ctx.Channel.Mention}!\nSome messages were not deleted because they are older than 2 weeks.")
                        .AsEphemeral());
                else
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"Cleared **{messagesToClear.Count}** messages from {ctx.Channel.Mention}!")
                        .AsEphemeral());
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "There were no messages that matched all of the arguments you provided! Nothing to do."));
            }
        }
    }

    [SlashCommand("kick", "Kick a user. They can rejoin the server with an invite.", false)]
    [SlashCommandPermissions(Permissions.KickMembers)]
    public async Task Kick(InteractionContext ctx, [Option("user", "The user to kick.")] DiscordUser userToKick,
        [Option("reason", "The reason for the kick.")]
        string reason = "No reason provided.")
    {
        DiscordMember memberToKick;
        try
        {
            memberToKick = await ctx.Guild.GetMemberAsync(userToKick.Id);
        }
        catch
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Hmm, **{userToKick.Username}#{userToKick.Discriminator}** doesn't seem to be in the server.")
                .AsEphemeral());
            return;
        }

        try
        {
            await memberToKick.RemoveAsync(reason);
        }
        catch
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to kick **{userToKick.Username}#{userToKick.Discriminator}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent("User kicked successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"{userToKick.Mention} has been kicked: **{reason}**");
    }

    [SlashCommand("ban", "Ban a user. They will not be able to rejoin unless unbanned.", false)]
    [SlashCommandPermissions(Permissions.BanMembers)]
    public async Task Ban(InteractionContext ctx, [Option("user", "The user to ban.")] DiscordUser userToBan,
        [Option("reason", "The reason for the ban.")]
        string reason = "No reason provided.")
    {
        try
        {
            await ctx.Guild.BanMemberAsync(userToBan.Id, 0, reason);
        }
        catch (UnauthorizedException)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to ban **{userToBan.Username}#{userToBan.Discriminator}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }
        catch (Exception e)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"Hmm, something went wrong while trying to ban that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                .AsEphemeral());
            return;
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
            .WithContent("User banned successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"{userToBan.Mention} has been banned: **{reason}**");
    }

    [SlashCommand("unban", "Unban a user.", false)]
    [SlashCommandPermissions(Permissions.BanMembers)]
    public async Task Unban(InteractionContext ctx, [Option("user", "The user to unban.")] DiscordUser userToUnban)
    {
        try
        {
            await ctx.Guild.UnbanMemberAsync(userToUnban);
        }
        catch (UnauthorizedException)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to unban **{userToUnban.Username}#{userToUnban.Discriminator}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }
        catch (Exception e)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"Hmm, something went wrong while trying to unban that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                .AsEphemeral());
            return;
        }

        await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
            $"Successfully unbanned **{userToUnban.Username}#{userToUnban.Discriminator}**!"));
    }

    [SlashCommand("nickname", "Changes my nickname.", false)]
    [SlashCommandPermissions(Permissions.ManageNicknames)]
    public async Task Nickname(InteractionContext ctx,
        [Option("nickname", "What to change my nickname to. Leave this blank to clear it.")]
        string nickname = null)
    {
        var bot = await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);
        await bot.ModifyAsync(x =>
        {
            x.Nickname = nickname;
            x.AuditLogReason = $"Nickname changed by {ctx.User.Username} (ID: {ctx.User.Id}).";
        });

        if (nickname != null)
            await ctx.CreateResponseAsync(
                new DiscordInteractionResponseBuilder().WithContent(
                    $"Nickname changed to **{nickname}** successfully!"));
        else
            await ctx.CreateResponseAsync(
                new DiscordInteractionResponseBuilder().WithContent("Nickname cleared successfully!"));
    }

    [SlashCommandGroup("lockdown", "Lock or unlock a channel.", false)]
    [SlashCommandPermissions(Permissions.ModerateMembers)]
    public class Lockdown
    {
        [SlashCommand("lock", "Lock a channel to prevent members from sending messages.")]
        public async Task Lock(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var existingOverwrites = ctx.Channel.PermissionOverwrites.ToArray();

            await ctx.Channel.AddOverwriteAsync(ctx.Member, Permissions.SendMessages);
            await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id),
                Permissions.SendMessages);
            await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages);

            foreach (var overwrite in existingOverwrites)
                if (overwrite.Type == OverwriteType.Role)
                {
                    if (await overwrite.GetRoleAsync() == ctx.Guild.EveryoneRole)
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            Permissions.SendMessages | overwrite.Denied);
                    else
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            overwrite.Denied);
                }
                else
                {
                    await ctx.Channel.AddOverwriteAsync(await overwrite.GetMemberAsync(), overwrite.Allowed,
                        overwrite.Denied);
                }

            await ctx.Channel.SendMessageAsync("This channel has been locked.");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Channel locked successfully.")
                .AsEphemeral());
        }

        [SlashCommand("unlock", "Unlock a locked channel to allow members to send messages again.")]
        public async Task Unlock(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            foreach (var permission in ctx.Channel.PermissionOverwrites.ToArray())
                if (permission.Type == OverwriteType.Role)
                {
                    if (await permission.GetRoleAsync() == ctx.Guild.EveryoneRole &&
                        permission.Denied.HasPermission(Permissions.SendMessages))
                    {
                        DiscordOverwriteBuilder newOverwrite = new(ctx.Guild.EveryoneRole)
                        {
                            Allowed = permission.Allowed,
                            Denied = (Permissions)(permission.Denied - Permissions.SendMessages)
                        };

                        await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, newOverwrite.Allowed,
                            newOverwrite.Denied);
                    }
                }
                else
                {
                    if (await permission.GetMemberAsync() ==
                        await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id) ||
                        await permission.GetMemberAsync() == ctx.Member)
                        await permission.DeleteAsync();
                }

            await ctx.Channel.SendMessageAsync("This channel has been unlocked.");
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Channel unlocked successfully.").AsEphemeral());
        }
    }

    [SlashCommandGroup("timeout", "Set or clear a timeout for a user.", false)]
    [SlashCommandPermissions(Permissions.ModerateMembers)]
    public class TimeoutCmds
    {
        [SlashCommand("set", "Time out a member.")]
        public async Task SetTimeout(InteractionContext ctx,
            [Option("member", "The member to time out.")]
            DiscordUser user,
            [Option("duration",
                "How long the timeout should last. Maximum value is 28 days due to Discord limitations.")]
            string duration,
            [Option("reason", "The reason for the timeout.")]
            string reason = "No reason provided.")
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            DiscordMember member;

            try
            {
                member = await ctx.Guild.GetMemberAsync(user.Id);
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Hmm. It doesn't look like that user is in the server, so I can't time them out.")
                    .AsEphemeral());
                return;
            }

            var parsedDuration = HumanDateParser.HumanDateParser.Parse(duration)
                .Subtract(ctx.Interaction.CreationTimestamp.DateTime);
            var expireTime = ctx.Interaction.CreationTimestamp.DateTime + parsedDuration;

            try
            {
                await member.TimeoutAsync(expireTime, reason);
            }
            catch (BadRequestException)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "It looks like you tried to set the timeout duration to more than 28 days in the future! Due to Discord limitations, timeouts can only be up to 28 days.")
                    .AsEphemeral());
                return;
            }
            catch (UnauthorizedException)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        $"Something went wrong. You or I may not be allowed to time out **{user.Username}#{user.Discriminator}**! Please check the role hierarchy and permissions.")
                    .AsEphemeral());
                return;
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"Hmm, something went wrong while trying to time out that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                    .AsEphemeral());
                return;
            }

            var dateToConvert = Convert.ToDateTime(expireTime);
            var unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Successfully timed out {user.Mention} until `{expireTime}` (<t:{unixTime}:R>)!")
                .AsEphemeral());
            await ctx.Channel.SendMessageAsync(
                $"{user.Mention} has been timed out, expiring <t:{unixTime}:R>: **{reason}**");
        }

        [SlashCommand("clear", "Clear a timeout before it's set to expire.")]
        public async Task ClearTimeout(InteractionContext ctx,
            [Option("member", "The member whose timeout to clear.")]
            DiscordUser user)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder());

            DiscordMember member;

            try
            {
                member = await ctx.Guild.GetMemberAsync(user.Id);
            }
            catch
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Hmm. It doesn't look like that user is in the server, so I can't remove their timeout."));
                return;
            }

            try
            {
                await member.TimeoutAsync(null, "Timeout cleared manually.");
            }
            catch (UnauthorizedException)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Something went wrong. You or I may not be allowed to clear the timeout for **{user.Username}#{user.Discriminator}**! Please check the role hierarchy and permissions."));
                return;
            }
            catch (Exception e)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        $"Hmm, something went wrong while trying to clear the timeout for that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                    .AsEphemeral());
                return;
            }

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().WithContent(
                    $"Successfully cleared the timeout for {user.Mention}!"));
        }
    }
}
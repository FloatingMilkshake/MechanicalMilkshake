﻿namespace MechanicalMilkshake.Events;

public class ErrorEvents
{
    public static async Task SlashCommandErrored(SlashCommandsExtension scmds, SlashCommandErrorEventArgs e)
    {
        switch (e.Exception)
        {
            case SlashExecutionChecksFailedException { FailedChecks: not null } execChecksFailedEx
                when execChecksFailedEx.FailedChecks.OfType<SlashRequireGuildAttribute>().Any():
                var noDmResponse = "This command cannot be used in DMs. Please use it in a server." +
                                   " Contact the bot owner if you need help or think I messed up" +
                                   $" (if you don't know who that is, see {SlashCmdMentionHelpers.GetSlashCmdMention("about")}!).";
                try
                {
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(noDmResponse).AsEphemeral());
                }
                catch
                {
                    await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(noDmResponse)
                        .AsEphemeral());
                }
                
                return;
            case SlashExecutionChecksFailedException:
                var cmdFailedResponse = $"Hmm, it looks like one of the checks for this command failed." +
                                        $" Make sure you and I both have the permissions required to use it," +
                                        $" and that you're using it properly. Contact the bot owner if you need help or" +
                                        $" think I messed up (if you don't know who that is, see {SlashCmdMentionHelpers.GetSlashCmdMention("about")}!).";
                try
                {
                    await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent(cmdFailedResponse).AsEphemeral());
                }
                catch
                {
                    await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(cmdFailedResponse)
                        .AsEphemeral());
                }
                
                return;
            default:
            {
                var exception = e.Exception;

                DiscordEmbedBuilder embed = new()
                {
                    Title = "An exception was thrown when executing a slash command",
                    Description =
                        $"An exception was thrown when {e.Context.User.Mention} used `/{e.Context.CommandName}`. Details are below.",
                    Color = DiscordColor.Red
                };
                embed.AddField("Exception Details",
                    $"```{exception.GetType()}: {exception.Message}:\n{exception.StackTrace}".Truncate(1020) + "\n```");

                // For /cdn and /link, respond directly to the interaction with the exception embed
                if (e.Context.CommandName is "cdn" or "link")
                {
                    // Try to respond to the interaction
                    try
                    {
                        await e.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
                    }
                    catch (BadRequestException)
                    {
                        // If the interaction was deferred, CreateResponseAsync() won't work
                        // Try using FollowUpAsync() instead
                        await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
                    }

                    return;
                }
                
                // For other commands, send the embed to the bot's home channel, and respond with a friendlier message

                var friendlyResponse = "It looks like this command is having issues! Sorry for the inconvenience."
                   + " Bot owners have been alerted. Try again in a few minutes, or DM a bot owner if this"
                   + $" keeps happening. See {SlashCmdMentionHelpers.GetSlashCmdMention("about")} for a list of owners.";

                // Try to respond to the interaction
                var failedToRespond = false;
                try
                {
                    await e.Context.CreateResponseAsync(
                        new DiscordInteractionResponseBuilder().WithContent(friendlyResponse));
                }
                catch (BadRequestException)
                {
                    try
                    {
                        // If the interaction was deferred, CreateResponseAsync() won't work
                        // Try using FollowUpAsync() instead
                        await e.Context.FollowUpAsync(
                            new DiscordFollowupMessageBuilder().WithContent(friendlyResponse));
                    }
                    catch
                    {
                        // Failed to respond to the interaction. Ignore and continue to send error to home channel
                        failedToRespond = true;
                    }
                }

                if (failedToRespond) embed.Description += " I was unable to respond to the interaction with an error.";
                
                // Send the exception embed to the bot's home channel
                await Program.HomeChannel.SendMessageAsync(embed);
                
                break;
            }
        }
    }

    // Most of the code for this exception handler is from Erisa's Cliptok: https://github.com/Erisa/Cliptok/blob/aabf8aa/Program.cs#L488-L527
    public static async Task CommandsNextService_CommandErrored(CommandsNextExtension cnext,
        CommandErrorEventArgs e)
    {
        if (e.Exception is CommandNotFoundException && (e.Command is null || e.Command.QualifiedName != "help"))
            return;

        List<Exception> exs = [];
        if (e.Exception is AggregateException ae)
            exs.AddRange(ae.InnerExceptions);
        else
            exs.Add(e.Exception);

        foreach (var ex in exs)
        {
            switch (ex)
            {
                case CommandNotFoundException when e.Command is null || e.Command.QualifiedName != "help":
                    return;
                case ChecksFailedException:
                    await e.Context.RespondAsync(
                        $"Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up (if you don't know who that is, see {SlashCmdMentionHelpers.GetSlashCmdMention("about")}!).");
                    return;
            }

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "An exception occurred when executing a command",
                Description = $"`{ex.GetType()}` occurred when executing `{e.Command!.QualifiedName}`.",
                Timestamp = DateTime.UtcNow
            };
            embed.AddField("Message", ex.Message);

            Console.WriteLine(
                $"{ex.GetType()} occurred when {UserInfoHelpers.GetFullUsername(e.Context.User)} used {e.Command!.QualifiedName}: {ex.Message}\n{ex.StackTrace}");

            // System.ArgumentException
            if (ex.GetType().ToString() == "System.ArgumentException")
                embed.AddField("What's that mean?", "This usually means that you used the command incorrectly.\n" +
                                                    $"Please run `help {e.Command!.QualifiedName}` for information about what you need to provide for the `{e.Command!.QualifiedName}` command.");

            // Check if bot has perms to send error response and send if so
            if (e.Context.Channel
                .PermissionsFor(await e.Context.Guild.GetMemberAsync(e.Context.Client.CurrentUser.Id))
                .HasPermission(Permissions.SendMessages))
                await e.Context.RespondAsync(embed.Build()).ConfigureAwait(false);
        }
    }
}
namespace MechanicalMilkshake.Events;

public class ErrorEvents
{
    public static async Task CommandErrored(CommandsExtension ext, CommandErroredEventArgs e)
    {
        switch (e.Context)
        {
            case MechanicalMilkshake.SlashCommandContext:
                await SlashCommandErrored(ext, e);
                break;
            case TextCommandContext:
                await TextCommandErrored(ext, e);
                break;
            default:
                await LogError(ext, e, true);
                break;
        }
    }
    
    /// <summary>
    /// Handler for database connection errors.
    /// </summary>
    /// <param name="ex">The exception thrown.</param>
    /// <returns>True if the exception was handled here. False otherwise.</returns>
    public static async Task<bool> DatabaseConnectionErrored(Exception ex)
    {
        // match timeout exceptions
        if (ex is RedisTimeoutException)
        {
            // use double.NaN as comparison value for unreachable
            var dbPing = double.NaN;
            try
            {
                // attempt to ping db
                dbPing = (await Program.Db.PingAsync()).TotalMilliseconds;
            }
            catch (RedisTimeoutException)
            {
                // db ping failed
                
                // if exceptions are already suppressed, don't report
                if (Program.RedisExceptionsSuppressed) return true;
                
                // report and suppress further timeout exceptions
                
                var ownerMention =
                    Program.Discord.CurrentApplication.Owners.Aggregate("",
                        (current, user) => current + user.Mention + " ");

                var pingMsg = double.IsNaN(dbPing)
                    ? "I couldn't ping Redis."
                    : $"Redis is reachable, and took {dbPing}ms to respond.";
                await Program.HomeChannel.SendMessageAsync(
                    $"{ownerMention} Redis is timing out! {pingMsg}" +
                    $" Database exceptions will be suppressed until the next check.",
                    embed: new DiscordEmbedBuilder
                    {
                        Color = DiscordColor.Red,
                        Title = "A database error occurred",
                        Description = $"```{ex.GetType()}: {ex.Message}\n{ex.StackTrace}```".Truncate(4096),
                    });
                
                Program.RedisExceptionsSuppressed = true;
            }

            return true;
        }

        return false;
    }
    
    private static async Task SlashCommandErrored(CommandsExtension ext, CommandErroredEventArgs e)
    {
        switch (e.Exception)
        {
            case ChecksFailedException execChecksFailedEx
                when execChecksFailedEx.Errors.Any(e => e.ContextCheckAttribute is RequireGuildAttribute):
                var noDmResponse = "This command cannot be used in DMs. Please use it in a server." +
                                   $" Need help? Contact a bot owner (see {SlashCmdMentionHelpers.GetSlashCmdMention("about")} for a list).";
                try
                {
                    await e.Context.As<MechanicalMilkshake.SlashCommandContext>().RespondAsync(noDmResponse, true);
                }
                catch
                {
                    await e.Context.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(noDmResponse)
                        .AsEphemeral());
                }
                
                return;
            case ChecksFailedException:
                var cmdFailedResponse = $"Hmm, it looks like one of the checks for this command failed." +
                                        $" Make sure you and I both have the permissions required to use it," +
                                        $" and that you're using it properly. Need help? Contact a bot owner" +
                                        $" (see {SlashCmdMentionHelpers.GetSlashCmdMention("about")} for a list).";
                try
                {
                    await e.Context.As<MechanicalMilkshake.SlashCommandContext>().RespondAsync(cmdFailedResponse, true);
                }
                catch
                {
                    await e.Context.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(cmdFailedResponse)
                        .AsEphemeral());
                }
                
                return;
            default:
            {
                await LogError(ext, e, true);
                break;
            }
        }
    }

    // Most of the code for this exception handler is from Erisa's Cliptok: https://github.com/Erisa/Cliptok/blob/aabf8aa/Program.cs#L488-L527
    private static async Task TextCommandErrored(CommandsExtension ext,
        CommandErroredEventArgs e)
    {
        if (e.Exception is CommandNotFoundException && (e.Context.Command.FullName != "help"))
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
                case CommandNotFoundException when e.Context.Command.FullName != "help":
                    return;
                case ChecksFailedException:
                    await e.Context.RespondAsync(
                        "Hmm, it looks like one of the checks for this command failed. " +
                        "Make sure you and I both have the permissions required to use it, " +
                        "and that you're using it properly. Need help? Contact a bot owner " +
                        $"(see {SlashCmdMentionHelpers.GetSlashCmdMention("about")} for a list).");
                    return;
            }

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "An exception occurred when executing a command",
                Description = $"`{ex.GetType()}` occurred when executing `{e.Context.Command.FullName}`.",
                Timestamp = DateTime.UtcNow
            };
            embed.AddField("Message", ex.Message);

            Program.Discord.Logger.LogError(Program.BotEventId, "An exception occurred when processing a text command!"
                + "\n{exType} occurred when {username} used {commandName}: {exMessage}\n{exStackTrace}", ex.GetType(), UserInfoHelpers.GetFullUsername(e.Context.User), e.Context.Command.FullName, ex.Message, ex.StackTrace);

            // System.ArgumentException
            if (ex.GetType().ToString() == "System.ArgumentException")
                embed.AddField("What's that mean?", "This usually means that you used the command incorrectly. Please try again.");

            // Check if bot has perms to send error response and send if so
            if (e.Context.Channel
                .PermissionsFor(await e.Context.Guild.GetMemberAsync(e.Context.Client.CurrentUser.Id))
                .HasPermission(DiscordPermission.SendMessages))
                await e.Context.RespondAsync(embed.Build()).ConfigureAwait(false);
        }
        
        await LogError(ext, e, false);
    }
    
    private static async Task LogError(CommandsExtension ext, CommandErroredEventArgs e, bool respond)
    {
        var exception = e.Exception;
        
        string commandName;
        try
        {
            commandName = e.Context.Command.FullName;
        }
        catch
        {
            try
            {
                commandName = e.Context.As<MechanicalMilkshake.SlashCommandContext>().Interaction.Data.Name;
            }
            catch
            {
                commandName = "<unknown>";
            }
        }

        DiscordEmbedBuilder embed = new()
        {
            Title = "An exception was thrown when executing a slash command",
            Description =
                $"An exception was thrown when {e.Context.User.Mention} used `{commandName}`. Details are below.",
            Color = DiscordColor.Red
        };
        embed.AddField("Exception Details",
            $"```{exception.GetType()}: {exception.Message}:\n{exception.StackTrace}".Truncate(1020) + "\n```");

        if (respond)
        {
            // For /cdn and /link, respond directly to the interaction with the exception embed
            if (e.Context.Command?.Name is "cdn" or "link" && e.Context is MechanicalMilkshake.SlashCommandContext)
            {
                // Try to respond to the interaction
                try
                {
                    await e.Context.RespondAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed));
                }
                catch (BadRequestException)
                {
                    // If the interaction was deferred, RespondAsync() won't work
                    // Try using FollowupAsync() instead
                    await e.Context.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
                }

                return;
            }
        
            // For other commands, send the embed to the bot's home channel, and respond with a friendlier message
            
            var friendlyResponse = "It looks like this command is having issues!"
               + " Try again in a few minutes, or DM a bot owner if this keeps happening."
               + $" See {SlashCmdMentionHelpers.GetSlashCmdMention("about")} for a list of owners.";
            
            // If command invoker is a bot owner, make the message more detailed
            if (Program.Discord.CurrentApplication.Owners.Any(owner => owner.Id == e.Context.User.Id))
            {
                friendlyResponse =
                    $"This command threw a(n) `{exception.GetType()}` exception. \"{exception.Message}\""
                    + $"\n\nHere's the stack trace:\n```{exception.StackTrace}```";
            }
            
            // We need to respond differently based on the type of command this is.
            if (e.Context is MechanicalMilkshake.SlashCommandContext)
            {
                // Try to respond to the interaction
                var failedToRespond = false;
                try
                {
                    await e.Context.RespondAsync(
                        new DiscordInteractionResponseBuilder().WithContent(friendlyResponse));
                }
                catch (Exception ex) when (ex is BadRequestException or InvalidOperationException)
                {
                    try
                    {
                        // If the interaction was deferred, RespondAsync() won't work
                        // Try using FollowupAsync() instead
                        await e.Context.FollowupAsync(
                            new DiscordFollowupMessageBuilder().WithContent(friendlyResponse));
                    }
                    catch
                    {
                        // Failed to respond to the interaction. Ignore and continue to send error to home channel
                        failedToRespond = true;
                    }
                }
                catch (Exception ex)
                {
                    // Failed to respond to the interaction for an unknown reason.
                    // Log the error anyway, plus this exception.
                    
                    embed.Description+= $" Additionally, an unknown exception occurred when trying to respond to the interaction: {ex.GetType()}: {ex.Message}";
                    await Program.HomeChannel.SendMessageAsync(embed);
                }
                
                if (failedToRespond) embed.Description += " I was unable to respond to the interaction with an error.";
            }
            else if (e.Context is TextCommandContext)
            {
                await e.Context.As<TextCommandContext>().Message.RespondAsync(friendlyResponse);
            }
            // else { ??? }
            // Currently MechanicalMilkshake.SlashCommandContext and TextCommandContext are the only derived classes of CommandContext
        }
        
        // Send the exception embed to the bot's home channel
        await Program.HomeChannel.SendMessageAsync(embed);
    }
}
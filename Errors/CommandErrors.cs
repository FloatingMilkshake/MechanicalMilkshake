namespace MechanicalMilkshake.Errors;

internal class CommandErrors
{
    internal static async Task HandleCommandErroredEventAsync(CommandsExtension _, CommandErroredEventArgs e)
    {
        // This should never happen, this bot is interaction-only and currently all other interaction-based
        // contexts are derived from SlashCommandContext
        if (e.Context is not SlashCommandContext)
            return;

        var context = e.Context.As<SlashCommandContext>();
        var userIsBotOwner = Setup.State.Discord.Client.CurrentApplication.Owners.Any(o => o.Id == context.User.Id);

        switch (e.Exception)
        {
            case CommandNotFoundException:
                {
                    if ((DateTime.Now - Setup.State.Process.ProcessStartTime).TotalMinutes < 2)
                    {
                        if (userIsBotOwner)
                            await TryToRespondToInteractionAsync(context.Interaction,
                                "Waiting for command registration, probably rate-limited. Wait a sec and try again.");
                        else
                            await TryToRespondToInteractionAsync(context.Interaction,
                                "Sorry, I'm waiting for Discord to process my commands! This command will be ready in a moment." +
                                " If you keep seeing this, please contact a bot owner for help!");
                    }
                    break;
                }
            case ChecksFailedException checksFailedException:
                {
                    if (checksFailedException.Errors.Any(e => e.ContextCheckAttribute is RequireBotCommanderAttribute or RequireApplicationOwnerAttribute))
                    {
                        await TryToRespondToInteractionAsync(context.Interaction, "Sorry, you aren't allowed to use this command!");
                    }
                    else if (checksFailedException.Errors.Any(e => e.ContextCheckAttribute is RequirePermissionsAttribute))
                    {
                        if (context.Interaction.ResponseState == DiscordInteractionResponseState.Unacknowledged)
                            await context.Interaction.DeferAsync();

                        var contextCheckFailedData = checksFailedException.Errors.First(e => e.ContextCheckAttribute is RequirePermissionsAttribute);
                        var requirePermissionsAttribute = contextCheckFailedData.ContextCheckAttribute as RequirePermissionsAttribute;
                        var requiredUserPermissions = requirePermissionsAttribute.UserPermissions;
                        var requiredBotPermissions = requirePermissionsAttribute.BotPermissions;
                        var currentUserPermissions = e.Context.Member.Permissions;
                        var currentBotPermissions = e.Context.Guild.CurrentMember.Permissions;

                        if (!requiredBotPermissions.HasAllPermissions(currentUserPermissions))
                        {
                            await TryToRespondToInteractionAsync(context.Interaction,
                                $"I don't have the right permissions for this command!\nPlease ask a server admin to grant me the following permissions: "
                                    + string.Join(", ", requiredBotPermissions.Except(currentBotPermissions)
                                        .Select(requiredPermission => $"**{requiredPermission.Humanize()}**")));
                        }
                        else if (!requiredBotPermissions.HasAllPermissions(currentUserPermissions))
                        {
                            await TryToRespondToInteractionAsync(context.Interaction,
                                $"You don't have permission to use this command!\nYou are missing the following permissions: "
                                    + string.Join(", ", requiredUserPermissions.Except(currentUserPermissions)
                                        .Select(requiredPermission => $"**{requiredPermission.Humanize()}**")));
                        }
                    }
                    else if (checksFailedException.Errors.Any(e => e.ContextCheckAttribute is RequireGuildAttribute))
                    {
                        await TryToRespondToInteractionAsync(context.Interaction, "This command must be used in a server!");
                    }
                    else if (checksFailedException.Errors.Any(e => e.ContextCheckAttribute is RequireHomeServerAttribute))
                    {
                        await TryToRespondToInteractionAsync(context.Interaction, "This command can only be used in the home server.");
                    }
                    else // Unexpected check failed
                    {
                        await TryToRespondToInteractionAsync(context.Interaction,
                            "Sorry, one of the checks for this command failed! Please make sure you are using it correctly." +
                            " If you keep seeing this, please contact a bot owner for help!");
                    }
                    break;
                }
            default:
                {
                    await TryToRespondToInteractionAsync(context.Interaction,
                        "An unexpected error occurred while running this command! Please try again." +
                        " If you keep seeing this, please contact a bot owner for help!");
                    break;
                }
        }

        await LogCommandErrorAsync(e);
    }

    private static async Task LogCommandErrorAsync(CommandErroredEventArgs e)
    {
        var commandName = e.Context.Command.FullName ?? "<unknown>";
        try
        {
            var embed = new DiscordEmbedBuilder()
            {
                Title = "An exception occurred during command execution",
                Description = $"An exception occurred when {e.Context.User.Username} (`{e.Context.User.Id}`) used `{commandName}`."
                    + $"\n```\n{e.Exception.GetType()}: {e.Exception.Message}\n{e.Exception.StackTrace}\n```".Truncate(3800, "...\n```")
            };
        }
        catch
        {
            // Oh well, still log to console
        }
        Setup.State.Discord.Client.Logger.LogError("An exception occurred during command execution! When {userId} used {commandName}:"
            + "\n{exceptionType}: {exceptionMessage}\n{exceptionStackTrace}", e.Context.User.Id, commandName,
            e.Exception.GetType(), e.Exception.Message, e.Exception.StackTrace);
    }

    private static async Task TryToRespondToInteractionAsync(DiscordInteraction interaction, string message)
    {
        var interactionResponseState = interaction.ResponseState;

        if (interactionResponseState != DiscordInteractionResponseState.Unacknowledged &&
            interactionResponseState != DiscordInteractionResponseState.Deferred &&
            interactionResponseState != DiscordInteractionResponseState.Replied)
        {
            return;
        }

        if (interactionResponseState == DiscordInteractionResponseState.Unacknowledged)
        {
            await interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(message));
        }
        else // Deferred
        {
            await interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent(message));
        }
    }
}

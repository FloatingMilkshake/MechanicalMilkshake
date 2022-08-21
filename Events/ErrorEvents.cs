﻿namespace MechanicalMilkshake.Events;

public class ErrorEvents
{
    public static async Task SlashCommandErrored(SlashCommandsExtension scmds, SlashCommandErrorEventArgs e)
    {
        if (e.Exception is SlashExecutionChecksFailedException exception)
        {
            if (exception.FailedChecks.OfType<SlashRequireGuildAttribute>().Any())
            {
                await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                    new DiscordInteractionResponseBuilder()
                        .WithContent(
                            "This command cannot be used in DMs. Please use it in a server. Contact the bot owner if you need help or think I messed up.")
                        .AsEphemeral());
                return;
            }

            await e.Context.CreateResponseAsync(InteractionResponseType.ChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().WithContent(
                        "Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up.")
                    .AsEphemeral());
            return;
        }

        List<Exception> exs = new();
        if (e.Exception is AggregateException ae)
            exs.AddRange(ae.InnerExceptions);
        else
            exs.Add(e.Exception);

        foreach (var ex in exs)
        {
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "An exception occurred when executing a slash command",
                Description = $"`{ex.GetType()}` occurred when executing `{e.Context.CommandName}`.",
                Timestamp = DateTime.UtcNow
            };
            embed.AddField("Message", ex.Message);

            // I don't know how to tell whether the command response was deferred or not, so we're going to try both an interaction response and follow-up so that the interaction doesn't time-out.
            try
            {
                await e.Context.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(embed.Build())
                    .AsEphemeral());
            }
            catch
            {
                // Sometimes a follow-up also doesn't work - if that's the case, we'll forward the error to the bot's home channel.
                try
                {
                    await e.Context.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed.Build())
                        .AsEphemeral());
                }
                catch
                {
                    embed.Description =
                        $"`{ex.GetType()}` occurred when {e.Context.User.Mention} used `{e.Context.CommandName}`.";
                    await Program.homeChannel.SendMessageAsync(embed);
                }
            }
        }
    }

    // Most of the code for this exception handler is from Erisa's Cliptok: https://github.com/Erisa/Cliptok/blob/aabf8aa/Program.cs#L488-L527
    public static async Task CommandsNextService_CommandErrored(CommandsNextExtension cnext,
        CommandErrorEventArgs e)
    {
        if (e.Exception is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
            return;

        List<Exception> exs = new();
        if (e.Exception is AggregateException ae)
            exs.AddRange(ae.InnerExceptions);
        else
            exs.Add(e.Exception);

        foreach (var ex in exs)
        {
            if (ex is CommandNotFoundException && (e.Command == null || e.Command.QualifiedName != "help"))
                return;

            if (ex is ChecksFailedException)
            {
                await e.Context.RespondAsync(
                    "Hmm, it looks like one of the checks for this command failed. Make sure you and I both have the permissions required to use it, and that you're using it properly. Contact the bot owner if you need help or think I messed up.");
                return;
            }

            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#FF0000"),
                Title = "An exception occurred when executing a command",
                Description = $"`{ex.GetType()}` occurred when executing `{e.Command.QualifiedName}`.",
                Timestamp = DateTime.UtcNow
            };
            embed.AddField("Message", ex.Message);

            // System.ArgumentException
            if (ex.GetType().ToString() == "System.ArgumentException")
                embed.AddField("What's that mean?", "This usually means that you used the command incorrectly.\n" +
                                                    $"Please run `help {e.Command.QualifiedName}` for information about what you need to provide for the `{e.Command.QualifiedName}` command.");

            // Check if bot has perms to send error response and send if so
            if (e.Context.Channel
                .PermissionsFor(await e.Context.Guild.GetMemberAsync(e.Context.Client.CurrentUser.Id))
                .HasPermission(Permissions.SendMessages))
                await e.Context.RespondAsync(embed.Build()).ConfigureAwait(false);
        }
    }
}
namespace MechanicalMilkshake.Tasks;

internal class ReminderTasks
{
    internal static async Task ExecuteAsync()
    {
        while (true)
        {
            await CheckRemindersAsync();
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    internal static async Task<(int numRemindersBefore, int numRemindersAfter, int numRemindersSent, int numRemindersFailed)> CheckRemindersAsync()
    {
        // keep some tallies to report back for manual checks
        var numRemindersSent = 0;
        var numRemindersFailed = 0;

        var reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");

        var numRemindersBefore = reminders.Length;

        foreach (var reminder in reminders.Select(r => JsonConvert.DeserializeObject<Setup.Types.Reminder>(r.Value)))
        {
            if (reminder.TriggerTime > DateTime.Now)
                continue;

            DiscordChannel reminderChannel = default;
            try
            {
                reminderChannel = await Setup.State.Discord.Client.GetChannelAsync(reminder.ChannelId);

                if (reminderChannel.Guild is null)
                    await SendReminderToDmAsync(reminder);
                else
                    await SendReminderToChannelAsync(reminder, reminderChannel);

                await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                numRemindersSent++;
            }
            catch (Exception e) when (e is UnauthorizedException or NotFoundException)
            {
                try
                {
                    // Couldn't send the reminder in the channel it was created in.
                    // Try to DM user instead.

                    await SendReminderToDmAsync(reminder);

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                    numRemindersSent++;
                }
                catch (Exception ex)
                {
                    // Couldn't DM user! Log error and delete reminder

                    await LogExceptionAsync(Setup.State.Discord.Channels.Home, ex);

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                    numRemindersFailed++;
                }
            }
            catch (Exception ex)
            {
                // Unexpected exception! Log error and delete reminder

                await LogExceptionAsync(Setup.State.Discord.Channels.Home, ex);

                await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                numRemindersFailed++;
            }
        }

        var numRemindersAfter = (int)await Setup.Storage.Redis.HashLengthAsync("reminders");

        return (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed);
    }

    private static async Task SendReminderToChannelAsync(Setup.Types.Reminder reminder, DiscordChannel channel)
    {
        var messageToSend = new DiscordMessageBuilder().WithContent($"<@{reminder.UserId}>, I have a reminder for you:");
        if (channel.PermissionsFor(channel.Guild.CurrentMember).HasPermission(DiscordPermission.EmbedLinks))
        {
            messageToSend.AddEmbed(reminder.CreateEmbed());
        }
        else
        {
            messageToSend.WithContent(messageToSend.Content
                + $"\n> **Reminder from <t:{reminder.GetSetTimeTimestamp()}:R>**"
                + (string.IsNullOrWhiteSpace(reminder.ReminderText) ? "" : $"\n> {reminder.ReminderText}")
                + $"\n> **Context:** {reminder.GetJumpLink()}");
        }
        await channel.SendMessageAsync(messageToSend.WithAllowedMentions([new UserMention(reminder.UserId)]));
    }

    private static async Task SendReminderToDmAsync(Setup.Types.Reminder reminder)
    {
        var messageToSend = new DiscordMessageBuilder().WithContent($"<@{reminder.UserId}>, I have a reminder for you:")
            .AddEmbed(reminder.CreateEmbed());

        var user = await Setup.State.Discord.Client.GetUserAsync(reminder.UserId);
        await user.SendMessageAsync(messageToSend.WithAllowedMentions([new UserMention(reminder.UserId)]));
    }

    private static async Task LogExceptionAsync(DiscordChannel logChannel, Exception ex)
    {
        DiscordEmbedBuilder errorEmbed = new()
        {
            Color = DiscordColor.Red,
            Title = "An exception occurred when checking reminders",
            Description = $"```\n{ex.GetType()}: {ex.Message}\n```"
        };

        Setup.State.Discord.Client.Logger.LogError(ex, "An exception occurred when checking reminders!");

        await logChannel.SendMessageAsync(errorEmbed);
    }
}

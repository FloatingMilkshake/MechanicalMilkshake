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

    internal static async Task<(int numRemindersBefore, int numRemindersAfter, int numRemindersSent, int numRemindersFailed, int numRemindersWithNullTime)> CheckRemindersAsync()
    {
        // keep some tallies to report back for manual checks
        var numRemindersSent = 0;
        var numRemindersFailed = 0;
        var numRemindersWithNullTime = 0;

        var reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");

        var numRemindersBefore = reminders.Length;

        foreach (var reminder in reminders.Select(r => JsonConvert.DeserializeObject<Setup.Types.Reminder>(r.Value)))
        {
            if (reminder.TriggerTime > DateTime.Now)
                continue;

            var messageToSend = new DiscordMessageBuilder().WithContent($"<@{reminder.UserId}>, I have a reminder for you:");

            var reminderEmbed = new DiscordEmbedBuilder()
            {
                Color = new DiscordColor("#7287fd"),
                Title = $"Reminder from <t:{reminder.GetSetTimeTimestamp()}:R>",
                Description = $"{reminder.ReminderText}"
            };

            string context;
            if (reminder.ReminderText.Equals("You set this reminder on a message with the \"Remind Me About This\" command."))
                context =
                    "This reminder was set privately, so I can't link back to the message where it was set!" +
                    $" However, [this link]({reminder.GetJumpLink()}) should show you messages around the time that you set the reminder.";
            else
                context = reminder.GetJumpLink();

            reminderEmbed.AddField("Context", context);

            DiscordChannel reminderChannel = default;
            try
            {
                reminderChannel = await Setup.State.Discord.Client.GetChannelAsync(reminder.ChannelId);
                if (reminderChannel != default &&
                reminderChannel.PermissionsFor(reminderChannel.Guild.CurrentMember).HasPermission(DiscordPermission.EmbedLinks))
                {
                    messageToSend.AddEmbed(reminderEmbed);
                }
                else
                {
                    messageToSend.WithContent(messageToSend.Content
                        + $"\n> **Reminder from <t:{reminder.GetSetTimeTimestamp()}:R>**"
                        + (string.IsNullOrWhiteSpace(reminder.ReminderText) ? "" : $"\n> {reminder.ReminderText}")
                        + $"\n> **Context:** {reminder.GetJumpLink()}");
                }

                var msg = await reminderChannel.SendMessageAsync(messageToSend.WithAllowedMentions([new UserMention(reminder.UserId)]));

                Setup.Types.Reminder.AddDelayEmbedField(reminderEmbed, msg.Id);
                await msg.ModifyAsync(msg.Content, reminderEmbed.Build());

                await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                numRemindersSent++;
            }
            catch (Exception e) when (e is UnauthorizedException or NotFoundException)
            {
                try
                {
                    // Couldn't send the reminder in the channel it was created in.
                    // Try to DM user instead.

                    var user = await Setup.State.Discord.Client.GetUserAsync(reminder.UserId);

                    var msg = await user.SendMessageAsync($"<@{reminder.UserId}>, I have a reminder for you:", reminderEmbed);

                    // add delay field with message id
                    Setup.Types.Reminder.AddDelayEmbedField(reminderEmbed, msg.Id);
                    await msg.ModifyAsync(msg.Content, reminderEmbed.Build());

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                    numRemindersSent++;
                }
                catch (Exception ex)
                {
                    // Couldn't DM user! Log error and delete reminder

                    await LogExceptionAsync(Setup.Configuration.Discord.Channels.Home, ex);

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                    numRemindersFailed++;
                }
            }
        }

        reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");
        var numRemindersAfter = reminders.Length;

        return (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed, numRemindersWithNullTime);
    }

    private static async Task LogExceptionAsync(DiscordChannel logChannel, Exception ex)
    {
        DiscordEmbedBuilder errorEmbed = new()
        {
            Color = DiscordColor.Red,
            Title = "An exception occurred when checking reminders",
            Description = $"`{ex.GetType()}: {ex.Message}`"
        };

        Setup.State.Discord.Client.Logger.LogError(ex, "An exception occurred when checking reminders!");

        await logChannel.SendMessageAsync(errorEmbed);
    }
}

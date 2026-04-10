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

            var reminderSetTimeTimestamp = ((DateTimeOffset)reminder.SetTime).ToUnixTimeSeconds();

            var messageToSend = new DiscordMessageBuilder().WithContent($"<@{reminder.UserId}>, I have a reminder for you:");

            DiscordChannel reminderChannel = default;
            try
            {
                reminderChannel = await Setup.State.Discord.Client.GetChannelAsync(reminder.ChannelId);
            }
            catch (Exception ex) when (ex is UnauthorizedException or NotFoundException)
            {
                // This is fine, we'll DM the user instead
            }

            DiscordEmbedBuilder embed = default;

            if (reminderChannel != default &&
                reminderChannel.PermissionsFor(reminderChannel.Guild.CurrentMember).HasPermission(DiscordPermission.EmbedLinks))
            {
                // Construct embed to send

                embed = new()
                {
                    Color = new DiscordColor("#7287fd"),
                    Title = $"Reminder from <t:{reminderSetTimeTimestamp}:R>",
                    Description = $"{reminder.ReminderText}"
                };

                string context;
                if (reminder.ReminderText.Equals("You set this reminder on a message with the \"Remind Me About This\" command."))
                    context =
                        "This reminder was set privately, so I can't link back to the message where it was set!" +
                        $" However, [this link](https://discord.com/channels/{reminder.GuildId}" +
                        $"/{reminder.ChannelId}/{reminder.MessageId}) should show you messages around the time" +
                        " that you set the reminder.";
                else
                    context = $"https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}";

                embed.AddField("Context", context);

                ReminderHelpers.AddReminderDelayEmbedField(embed);

                messageToSend.AddEmbed(embed);
            }
            else
            {
                messageToSend.WithContent(messageToSend.Content
                    + $"\n> **Reminder from <t:{reminderSetTimeTimestamp}:R>**"
                    + (string.IsNullOrWhiteSpace(reminder.ReminderText) ? "" : $"\n> {reminder.ReminderText}")
                    + $"\n> **Context:** https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}");
            }

            try
            {
                var msg = await reminderChannel.SendMessageAsync(messageToSend);

                if (embed != default)
                {
                    embed.RemoveFieldAt(1);
                    ReminderHelpers.AddReminderDelayEmbedField(embed, msg.Id);
                    await msg.ModifyAsync(msg.Content, embed.Build());
                }

                await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                numRemindersSent++;
            }
            catch
            {
                try
                {
                    // Couldn't send the reminder in the channel it was created in.
                    // Try to DM user instead.

                    var user = await Setup.State.Discord.Client.GetUserAsync(reminder.UserId);

                    if (embed == default)
                    {
                        embed = new()
                        {
                            Color = new DiscordColor("#7287fd"),
                            Title = $"Reminder from <t:{reminderSetTimeTimestamp}:R>",
                            Description = $"{reminder.ReminderText}"
                        };

                        string context;
                        if (reminder.ReminderText.Equals("You set this reminder on a message with the \"Remind Me About This\" command."))
                            context =
                                "This reminder was set privately, so I can't link back to the message where it was set!" +
                                $" However, [this link](https://discord.com/channels/{reminder.GuildId}" +
                                $"/{reminder.ChannelId}/{reminder.MessageId}) should show you messages around the time" +
                                " that you set the reminder.";
                        else
                            context = $"https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId}";

                        embed.AddField("Context", context);

                        ReminderHelpers.AddReminderDelayEmbedField(embed);
                    }

                    var msg = await user.SendMessageAsync( $"<@{reminder.UserId}>, I have a reminder for you:", embed);

                    // update delay field to include message id
                    embed.RemoveFieldAt(1);
                    ReminderHelpers.AddReminderDelayEmbedField(embed, msg.Id);
                    await msg.ModifyAsync(msg.Content, embed.Build());

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                    numRemindersSent++;
                }
                catch (Exception ex)
                {
                    // Couldn't DM user! Log error and delete reminder

                    await ReminderHelpers.LogReminderErrorAsync(Setup.Configuration.Discord.Channels.Home, ex);

                    await Setup.Storage.Redis.HashDeleteAsync("reminders", reminder.ReminderId);

                    numRemindersFailed++;
                }
            }
        }

        reminders = await Setup.Storage.Redis.HashGetAllAsync("reminders");
        var numRemindersAfter = reminders.Length;

        return (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed, numRemindersWithNullTime);
    }
}

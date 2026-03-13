namespace MechanicalMilkshake.Events;

public class ModalEvents
{
    public static async Task ModalSubmitted(DiscordClient _, ModalSubmittedEventArgs e)
    {
        switch (e.Id)
        {
            case "remind-me-about-this-modal":
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().AsEphemeral(true));

                    var targetMessage = Commands.ContextMenuCommands.ReminderCommands.ReminderInteractionCache[e.Interaction.User.Id];

                    var timeInput = (e.Values["remind-me-about-this-time-input"] as TextInputModalSubmission).Value;

                    DateTime time;
                    try
                    {
                        time = HumanDateParser.HumanDateParser.Parse(timeInput);
                    }
                    catch
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                            .WithContent($"I couldn't parse \"{timeInput}\" as a time! Please try again."));
                        return;
                    }

                    // Create reminder

                    Random random = new();
                    var reminderId = random.Next(1000, 9999);

                    var reminders = await Program.Db.HashGetAllAsync("reminders");
                    foreach (var rem in reminders)
                        while (rem.Name == reminderId)
                            reminderId = random.Next(1000, 9999);

                    var reminder = new Entities.Reminder
                    {
                        UserId = e.Interaction.User.Id,
                        ChannelId = e.Interaction.Channel.Id,
                        MessageId = targetMessage.Id,
                        SetTime = DateTime.Now,
                        ReminderTime = time,
                        ReminderId = reminderId,
                        ReminderText = "You set this reminder on a message with the \"Remind Me About This\" command.",
                        GuildId = e.Interaction.Guild is null ? "@me" : e.Interaction.Guild.Id.ToString()
                    };

                    // Save reminder to db
                    await Program.Db.HashSetAsync("reminders", reminderId.ToString(), JsonConvert.SerializeObject(reminder));

                    // Respond
                    var unixTime = ((DateTimeOffset)time).ToUnixTimeSeconds();
                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent($"Alright, I will remind you about [that message](<{targetMessage.JumpLink}>) on <t:{unixTime}:F> (<t:{unixTime}:R>)."));

                    break;
                }
            case "reminder-modify-modal":
                {
                    await e.Interaction.DeferAsync(true);

                    var time = (e.Values["reminder-modify-time-input"] as TextInputModalSubmission).Value;
                    var text = (e.Values["reminder-modify-text-input"] as TextInputModalSubmission).Value;

                    var reminder = ComponentInteractionEvent.ReminderModifyCache[e.Interaction.User.Id];

                    if (string.IsNullOrWhiteSpace(text) && string.IsNullOrWhiteSpace(time))
                    {
                        await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder().WithContent("Reminder unchanged."));
                        return;
                    }

                    if (!string.IsNullOrWhiteSpace(text)) reminder.ReminderText = text;

                    if (!string.IsNullOrWhiteSpace(time))
                        try
                        {
                            reminder.ReminderTime = HumanDateParser.HumanDateParser.Parse(time);
                        }
                        catch
                        {
                            // Parse error, either because the user did it wrong or because HumanDateParser is weird

                            await e.Interaction.CreateFollowupMessageAsync(
                                new DiscordFollowupMessageBuilder().WithContent(
                                    $"I couldn't parse \"{time}\" as a time! Please try again."));
                            return;
                        }

                    await Program.Db.HashSetAsync("reminders", reminder.ReminderId, JsonConvert.SerializeObject(reminder));

                    if (!reminder.IsPrivate && reminder.ReminderTime is not null)
                    {
                        try
                        {
                            var reminderChannel = await Program.Discord.GetChannelAsync(reminder.ChannelId);

                            if (reminder.MessageId != default)
                            {
                                var reminderMessage = await reminderChannel.GetMessageAsync(reminder.MessageId);

                                var unixTime = ((DateTimeOffset)reminder.ReminderTime).ToUnixTimeSeconds();

                                if (reminderMessage.Content.Contains("pushed back"))
                                    await reminderMessage.ModifyAsync(
                                        $"[Reminder](https://discord.com/channels/{reminder.GuildId}/{reminder.ChannelId}/{reminder.MessageId})" +
                                        $" pushed back to <t:{unixTime}:F> (<t:{unixTime}:R>)!" +
                                        $"\nReminder ID: `{reminder.ReminderId}`");
                                else
                                    await reminderMessage.ModifyAsync($"Reminder set for <t:{unixTime}:F> (<t:{unixTime}:R>)!" +
                                                                      $"\nReminder ID: `{reminder.ReminderId}`");
                            }
                        }
                        catch
                        {
                            // Not important
                        }
                    }

                    await e.Interaction.CreateFollowupMessageAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("Reminder modified successfully."));

                    break;
                }
            default:
                {
                    await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.ChannelMessageWithSource,
                        new DiscordInteractionResponseBuilder().WithContent($"Unknown interaction ID `{e.Id}`! Please contact a bot owner for help."));
                    break;
                }
        }
    }
}

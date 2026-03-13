namespace MechanicalMilkshake.Events;

public class ModalEvents
{
    public static async Task ModalSubmitted(DiscordClient _, ModalSubmittedEventArgs e)
    {
        await e.Interaction.CreateResponseAsync(DiscordInteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral(true));

        var targetMessage = Commands.ContextMenuCommands.Reminder.ReminderInteractionCache[e.Interaction.User.Id];

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
    }
}

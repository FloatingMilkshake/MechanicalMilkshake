namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class Reminder
{

    [Command("Remind Me About This")]
    [AllowedProcessors(typeof(MessageCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    public static async Task ContextReminder(CommandContext ctx, DiscordMessage targetMessage)
    {
        await ctx.RespondAsync($"OK {ctx.User.Mention}, I can remind you about [that message](<{targetMessage.JumpLink}>). When would you like to be reminded?");
        
        var msg = await ctx.GetResponseAsync();

        var nextMsg = await msg.Channel.GetNextMessageAsync(m => m.Author.Id == ctx.User.Id);

        DateTime time;
        try
        {
            time = HumanDateParser.HumanDateParser.Parse(nextMsg.Result.Content);
        }
        catch
        {
            await nextMsg.Result.RespondAsync($"I couldn't parse \"{nextMsg.Result.Content}\" as a time! Please try again.");
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
            UserId = ctx.User.Id,
            ChannelId = ctx.Channel.Id,
            MessageId = targetMessage.Id,
            SetTime = DateTime.Now,
            ReminderTime = time,
            ReminderId = reminderId,
            ReminderText = "You set this reminder on a message with the \"Remind Me About This\" command.",
            GuildId = ctx.Guild.Id.ToString()
        };
        
        // Save reminder to db
        await Program.Db.HashSetAsync("reminders", reminderId.ToString(), JsonConvert.SerializeObject(reminder));
        
        // Respond
        var unixTime = ((DateTimeOffset)time).ToUnixTimeSeconds();
        await nextMsg.Result.RespondAsync($"Alright, I will remind you about [that message](<{targetMessage.JumpLink}>) on <t:{unixTime}:F> (<t:{unixTime}:R>).");
    }
}
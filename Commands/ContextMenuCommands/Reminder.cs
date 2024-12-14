namespace MechanicalMilkshake.Commands.ContextMenuCommands;

public class Reminder
{

    [Command("Remind Me About This")]
    [AllowedProcessors(typeof(MessageCommandProcessor))]
    [SlashCommandTypes(DiscordApplicationCommandType.MessageContextMenu)]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]

    public static async Task ContextReminder(MessageCommandContext ctx, DiscordMessage targetMessage)
    {
        DiscordChannel responseChannel;
        if (ctx.Interaction.Guild is null)
        {
            try
            {
                responseChannel = (await ctx.User.SendMessageAsync($"OK {ctx.User.Mention}, I can remind you about [that message](<{targetMessage.JumpLink}>). When would you like to be reminded?")).Channel;
                await ctx.RespondAsync(new DiscordInteractionResponseBuilder().WithContent("Check your DMs!").AsEphemeral());
            }
            catch
            {
                await ctx.RespondAsync("Sorry, that didn't work! Try using `/reminder set` instead.");
                return;
            }
        }
        else
        {
            await ctx.RespondAsync($"OK {ctx.User.Mention}, I can remind you about [that message](<{targetMessage.JumpLink}>). When would you like to be reminded?");
            responseChannel = ctx.Channel;
        }
        
        var nextMsg = await responseChannel.GetNextMessageAsync(m => m.Author.Id == ctx.User.Id);

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
            GuildId = ctx.Guild is null ? "@me" : ctx.Guild.Id.ToString()
        };
        
        // Save reminder to db
        await Program.Db.HashSetAsync("reminders", reminderId.ToString(), JsonConvert.SerializeObject(reminder));
        
        // Respond
        var unixTime = ((DateTimeOffset)time).ToUnixTimeSeconds();
        await nextMsg.Result.RespondAsync($"Alright, I will remind you about [that message](<{targetMessage.JumpLink}>) on <t:{unixTime}:F> (<t:{unixTime}:R>).");
    }
}
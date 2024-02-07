namespace MechanicalMilkshake.Checks;

public class ReminderChecks
{
    public static async Task<(int numRemindersBefore, int numRemindersAfter, int numRemindersSent, int numRemindersFailed)> CheckRemindersAsync()
    {
        // keep some tallies to report back for manual checks
        var numRemindersSent = 0;
        var numRemindersFailed = 0;
        
        // FETCH REMINDERS
        
        // get reminders from db
        var reminders = await Program.Db.HashGetAllAsync("reminders");

        // get number of reminders in db before check
        var numRemindersBefore = reminders.Length;

        // iterate through reminders
        foreach (var reminder in reminders)
        {
            // deserialize reminder
            var reminderData = JsonConvert.DeserializeObject<Reminder>(reminder.Value);

            // skip if reminder time is null or in the future
            // (this reminder will be re-checked and sent later)
            if (reminderData!.ReminderTime is null) continue;
            if (reminderData!.ReminderTime > DateTime.Now) continue;
            
            // BUILD MESSAGE

            // get time reminder was set and start building embed
            var setTime = ((DateTimeOffset)reminderData.SetTime).ToUnixTimeSeconds();
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#7287fd"),
                Title = $"Reminder from <t:{setTime}:R>",
                Description = $"{reminderData.ReminderText}"
            };

            // add context field
            
            string context;
            if (reminderData.IsPrivate)
                context =
                    "This reminder was set privately, so I can't link back to the message where it was set!" +
                    $" However, [this link](https://discord.com/channels/{reminderData.GuildId}" +
                    $"/{reminderData.ChannelId}/{reminderData.MessageId}) should show you messages around the time" +
                    " that you set the reminder.";
            else
                context =
                    $"[Jump Link](https://discord.com/channels/{reminderData.GuildId}/{reminderData.ChannelId}" +
                    $"/{reminderData.MessageId})";

            embed.AddField("Context", context);

            // add pushback field
            AddReminderPushbackEmbedField(embed);
            
            // GET USER

            // try to fetch user to send reminder to in case of private reminder or if channel send fails
            DiscordUser user;
            try
            {
                // try to fetch user
                user = await Program.Discord.GetUserAsync(reminderData.UserId);
            }
            catch
            {
                // user cannot be fetched
                // reminder can still be sent in channel, but not privately!
                user = default;
            }
            
            // get server member if user was able to be fetched
            DiscordMember targetMember = default;
            if (user != default)
            {
                // try to find mutual server with user; iterate through guilds to find user, then get member from guild if found
                DiscordGuild mutualServer = default;
                foreach (var guild in Program.Discord.Guilds)
                    if (guild.Value.Members.Any(m =>
                        m.Value.Username == user.Username && UserInfoHelpers.GetDiscriminator(m.Value) == UserInfoHelpers.GetDiscriminator(user)))
                    {
                        mutualServer = await Program.Discord.GetGuildAsync(guild.Value.Id);
                        break;
                    }

                // no mutual server found
                if (mutualServer == default)
                {
                    // mutual server could not be found, so member cannot be fetched.
                    // reminder can still be sent in channel, but not privately!
                }
                else
                {
                    // mutual server found; get member
                    targetMember = await mutualServer.GetMemberAsync(user.Id);
                }
            }
            
            // SEND REMINDER
            
            // PRIVATE REMINDERS
            
            // if reminder is private, try to send in DM
            if (reminderData.IsPrivate)
            {
                try
                {
                    // send dm to user
                    var msg = await targetMember.SendMessageAsync(
                        $"<@{reminderData.UserId}>, I have a reminder for you:",
                        embed);

                    // update pushback field to include message id
                    embed.RemoveFieldAt(1);
                    AddReminderPushbackEmbedField(embed, msg.Id);
                    await msg.ModifyAsync(msg.Content, embed.Build());

                    // delete reminder from db
                    await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);

                    // increment sent reminder count
                    numRemindersSent++;

                    // continue to next reminder
                    continue;
                }
                catch
                {
                    // Couldn't DM user for private reminder - DMs are disabled or bot is blocked. Try to ping in public channel.
                    // (we are not sending the reminder content publicly here in case of sensitive information; just alert the user)
                    try
                    {
                        var reminderChannel = await Program.Discord.GetChannelAsync(reminderData.ChannelId);
                        var msgContent =
                            $"Hi {targetMember.Mention}! I have a reminder for you. I tried to DM it to you, but either your DMs are off or I am blocked." +
                            " Please make sure that I can DM you.\n\n**Your reminder will not be sent automatically following this alert.**" +
                            $" You can use the `/reminder` commands to show, modify, or delete it.";

                        await reminderChannel.SendMessageAsync(msgContent);

                        // set reminder time to null so that it will never be automatically sent
                        // user must modify or delete it manually
                        reminderData.ReminderTime = null;
                        await Program.Db.HashSetAsync("reminders", reminderData.ReminderId,
                            JsonConvert.SerializeObject(reminderData));
                        
                        // continue to next reminder
                        continue;
                    }
                    catch (Exception ex)
                    {
                        // Couldn't DM user or send an alert in the channel the reminder was set in!
                        // Log error and delete reminder to prevent error spam

                        await LogReminderError(Program.HomeChannel, ex);

                        await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);
                        
                        // increment failed reminder count
                        numRemindersFailed++;
                        
                        // continue to next reminder
                        continue;
                    }
                }
            }
            
            // PUBLIC REMINDERS

            // reminder is not private; try to send in channel
            try
            {
                // fetch channel and send reminder
                var targetChannel = await Program.Discord.GetChannelAsync(reminderData.ChannelId);
                var msg = await targetChannel.SendMessageAsync(
                    $"<@{reminderData.UserId}>, I have a reminder for you:",
                    embed);

                // update pushback field to include message id
                embed.RemoveFieldAt(1);
                AddReminderPushbackEmbedField(embed, msg.Id);
                await msg.ModifyAsync(msg.Content, embed.Build());

                // delete reminder from db
                await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);

                // increment sent reminder count
                numRemindersSent++;
            }
            catch // failed to send in channel
            {
                try
                {
                    // Couldn't send the reminder in the channel it was created in.
                    // Try to DM user instead.

                    // send dm
                    var msg = await targetMember.SendMessageAsync(
                        $"<@{reminderData.UserId}>, I have a reminder for you:",
                        embed);

                    // update pushback field to include message id
                    embed.RemoveFieldAt(1);
                    AddReminderPushbackEmbedField(embed, msg.Id);
                    await msg.ModifyAsync(msg.Content, embed.Build());

                    // delete reminder from db
                    await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);

                    // increment sent reminder count
                    numRemindersSent++;
                }
                catch (Exception ex)
                {
                    // Couldn't DM user! Log error and delete reminder to prevent error spam

                    await LogReminderError(Program.HomeChannel, ex);

                    await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);
                    
                    // increment failed reminder count
                    numRemindersFailed++;
                }
            }
        }
        
        // get number of reminders in db after check
        reminders = await Program.Db.HashGetAllAsync("reminders");
        var numRemindersAfter = reminders.Length;
        
        return (numRemindersBefore, numRemindersAfter, numRemindersSent, numRemindersFailed);
    }

    private static async Task LogReminderError(DiscordChannel logChannel, Exception ex)
    {
        DiscordEmbedBuilder errorEmbed = new()
        {
            Color = DiscordColor.Red,
            Title = "An exception occurred when checking reminders",
            Description =
                $"`{ex.GetType()}` occurred when checking for overdue reminders."
        };
        errorEmbed.AddField("Message", $"{ex.Message}");
        errorEmbed.AddField("Stack Trace", $"```\n{ex.StackTrace}\n```");

        Console.WriteLine(
            $"{ex.GetType()} occurred when checking reminders: {ex.Message}\n{ex.StackTrace}");

        await logChannel.SendMessageAsync(errorEmbed);
    }

    private static void AddReminderPushbackEmbedField(DiscordEmbedBuilder embed, ulong msgId = default)
    {
        var id = msgId == default ? "[loading...]" : $"`{msgId}`";

        embed.AddField("Need to delay this reminder?",
            $"Use {SlashCmdMentionHelpers.GetSlashCmdMention("reminder", "pushback")}and set `message` to {id}.");
    }
}
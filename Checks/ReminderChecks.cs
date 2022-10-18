namespace MechanicalMilkshake.Checks;

public class ReminderChecks
{
    public static async Task<bool> ReminderCheck()
    {
        var reminders = await Program.Db.HashGetAllAsync("reminders");

        foreach (var reminder in reminders)
        {
            var reminderData = JsonConvert.DeserializeObject<Reminder>(reminder.Value);

            if (reminderData!.ReminderTime is null) continue;

            if (reminderData!.ReminderTime > DateTime.Now) continue;
            var setTime = ((DateTimeOffset)reminderData.SetTime).ToUnixTimeSeconds();
            DiscordEmbedBuilder embed = new()
            {
                Color = new DiscordColor("#7287fd"),
                Title = $"Reminder from <t:{setTime}:R>",
                Description = $"{reminderData.ReminderText}"
            };

            string context;
            if (reminderData.IsPrivate)
                context =
                    "This reminder was set privately, so I can't link back to the message where it was set!" +
                    $" However, [this link](https://discord.com/channels/{reminderData.GuildId}/{reminderData.ChannelId}/{reminderData.MessageId}) should show you messages around the time that you set the reminder.";
            else
                context =
                    $"[Jump Link](https://discord.com/channels/{reminderData.GuildId}/{reminderData.ChannelId}/{reminderData.MessageId})";

            embed.AddField("Context", context);

            var reminderCommand = Program.ApplicationCommands.FirstOrDefault(sc => sc.Name == "reminder");

            if (reminderCommand is not null)
                embed.AddField("Need to delay this reminder?",
                    $"Use </{reminderCommand.Name} pushback:{reminderCommand.Id}> and set `message` to [loading...].");

            var guildId = Convert.ToUInt64(reminderData.GuildId);
            var guild = await Program.Discord.GetGuildAsync(guildId);
            var targetMember = await guild.GetMemberAsync(reminderData.UserId);

            if (reminderData.IsPrivate)
                try
                {
                    // Try to DM user for private reminder

                    var msg = await targetMember.SendMessageAsync(
                        $"<@{reminderData.UserId}>, I have a reminder for you:",
                        embed);

                    embed.RemoveFieldAt(1);

                    if (reminderCommand is not null)
                        embed.AddField("Need to delay this reminder?",
                            $"Use </{reminderCommand.Name} pushback:{reminderCommand.Id}> and set `message` to `{msg.Id}`.");

                    await msg.ModifyAsync(msg.Content, embed.Build());

                    await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);

                    return true;
                }
                catch
                {
                    // Couldn't DM user for private reminder - DMs are disabled or bot is blocked. Try to ping in public channel.
                    try
                    {
                        var reminderCmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == "reminder");

                        var reminderChannel = await Program.Discord.GetChannelAsync(reminderData.ChannelId);
                        var msgContent =
                            $"Hi {targetMember.Mention}! I have a reminder for you. I tried to DM it to you, but either your DMs are off or I am blocked." +
                            " Please make sure that I can DM you.\n\n**Your reminder will not be sent automatically following this alert.**";

                        if (reminderCmd is not null)
                            msgContent +=
                                $" You can use </{reminderCmd.Name} modify:{reminderCmd.Id}> to modify your reminder, or </{reminderCmd.Name} delete:{reminderCmd.Id}> to delete it.";

                        await reminderChannel.SendMessageAsync(msgContent);

                        reminderData.ReminderTime = null;
                        await Program.Db.HashSetAsync("reminders", reminderData.ReminderId,
                            JsonConvert.SerializeObject(reminderData));

                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Couldn't DM user or send an alert in the channel the reminder was set in... log error

                        await LogReminderError(Program.HomeChannel, ex);

                        return false;
                    }
                }

            try
            {
                var targetChannel = await Program.Discord.GetChannelAsync(reminderData.ChannelId);
                var msg = await targetChannel.SendMessageAsync(
                    $"<@{reminderData.UserId}>, I have a reminder for you:",
                    embed);

                embed.RemoveFieldAt(1);

                if (reminderCommand is not null)
                    embed.AddField("Need to delay this reminder?",
                        $"Use </{reminderCommand.Name} pushback:{reminderCommand.Id}> and set `message` to `{msg.Id}`.");

                await msg.ModifyAsync(msg.Content, embed.Build());

                await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);

                return true;
            }
            catch
            {
                try
                {
                    // Couldn't send the reminder in the channel it was created in.
                    // Try to DM user instead.

                    var msg = await targetMember.SendMessageAsync(
                        $"<@{reminderData.UserId}>, I have a reminder for you:",
                        embed);

                    embed.RemoveFieldAt(1);

                    if (reminderCommand is not null)
                        embed.AddField("Need to delay this reminder?",
                            $"Use </{reminderCommand.Name} pushback:{reminderCommand.Id}> and set `message` to `{msg.Id}`.");

                    await msg.ModifyAsync(msg.Content, embed.Build());

                    await Program.Db.HashDeleteAsync("reminders", reminderData.ReminderId);

                    return true;
                }
                catch (Exception ex)
                {
                    // Couldn't DM user. Log error.

                    await LogReminderError(Program.HomeChannel, ex);

                    return false;
                }
            }
        }

        return true;
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

        await logChannel.SendMessageAsync(errorEmbed);
    }
}
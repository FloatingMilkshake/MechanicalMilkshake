namespace MechanicalMilkshake.Checks
{
    public class ReminderChecks
    {
        public static async Task<bool> ReminderCheck()
        {
            var reminders = await Program.db.HashGetAllAsync("reminders");

            foreach (var reminder in reminders)
            {
                var reminderData = JsonConvert.DeserializeObject<Reminder>(reminder.Value);

                if (reminderData.ReminderTime <= DateTime.Now)
                {
                    var setTime = ((DateTimeOffset)reminderData.SetTime).ToUnixTimeSeconds();
                    DiscordEmbedBuilder embed = new()
                    {
                        Color = new DiscordColor("#7287fd"),
                        Title = $"Reminder from <t:{setTime}:R>",
                        Description = $"{reminderData.ReminderText}"
                    };
                    embed.AddField("Context",
                        $"[Jump Link](https://discord.com/channels/{reminderData.GuildId}/{reminderData.ChannelId}/{reminderData.MessageId})");

#if DEBUG
                    var slashCommands =
                        await Program.discord.GetGuildApplicationCommandsAsync(Program.configjson.Base.HomeServerId);
#else
                var slashCommands = await Program.discord.GetGlobalApplicationCommandsAsync();
#endif
                    var reminderCommand = slashCommands.Where(sc => sc.Name == "reminder").FirstOrDefault();
                    var reminderPushbackCommand =
                        reminderCommand.Options.Where(opt => opt.Name == "pushback").FirstOrDefault();

                    embed.AddField("Need to delay this reminder?",
                        $"Use </{reminderCommand.Name} {reminderPushbackCommand.Name}:{reminderCommand.Id}> and set `message` to [loading...].");

                    try
                    {
                        var targetChannel = await Program.discord.GetChannelAsync(reminderData.ChannelId);
                        var msg = await targetChannel.SendMessageAsync(
                            $"<@{reminderData.UserId}>, I have a reminder for you:",
                            embed);

                        embed.RemoveFieldAt(1);
                        embed.AddField("Need to delay this reminder?",
                            $"Use </{reminderCommand.Name} {reminderPushbackCommand.Name}:{reminderCommand.Id}> and set `message` to `{msg.Id}`.");

                        await msg.ModifyAsync(msg.Content, embed.Build());

                        await Program.db.HashDeleteAsync("reminders", reminderData.ReminderId);

                        return true;
                    }
                    catch
                    {
                        try
                        {
                            // Couldn't send the reminder in the channel it was created in.
                            // Try to DM user instead.

                            var guildId = Convert.ToUInt64(reminderData.GuildId);
                            var guild = await Program.discord.GetGuildAsync(guildId);
                            var targetMember = await guild.GetMemberAsync(reminderData.UserId);

                            var msg = await targetMember.SendMessageAsync(
                                $"<@{reminderData.UserId}>, I have a reminder for you:",
                                embed);

                            embed.RemoveFieldAt(1);
                            embed.AddField("Need to delay this reminder?",
                                $"Use </{reminderCommand.Name} {reminderPushbackCommand.Name}:{reminderCommand.Id}> and set `message` to `{msg.Id}`.");

                            await msg.ModifyAsync(msg.Content, embed.Build());

                            await Program.db.HashDeleteAsync("reminders", reminderData.ReminderId);

                            return true;
                        }
                        catch (Exception ex)
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

                            await Program.homeChannel.SendMessageAsync(errorEmbed);

                            return false;
                        }
                    }
                }
            }

            return true;
        }
    }
}

namespace MechanicalMilkshake;

public class Checks
{
    public static async Task PackageUpdateCheck()
    {
#if DEBUG
        Program.discord.Logger.LogInformation(Program.BotEventId, "PackageUpdateCheck running.");
#endif
        var updatesAvailableResponse = "";
        var restartRequiredResponse = "";

        EvalCommands evalCommands = new();
        var updatesAvailable = false;
        var restartRequired = false;
        foreach (var host in Program.configjson.SshHosts)
        {
#if DEBUG
            Program.discord.Logger.LogInformation(Program.BotEventId,
                "[PackageUpdateCheck] Checking for updates on host '{host}'.\"", host);
#endif
            var cmdResult =
                await evalCommands.RunCommand($"ssh {host} \"cat /var/run/reboot-required ; sudo apt update\"");
            if (cmdResult.Contains(" can be upgraded"))
            {
                updatesAvailableResponse += $"`{host}`\n";
                updatesAvailable = true;
            }

            if (cmdResult.Contains("System restart required")) restartRequired = true;
        }
#if DEBUG
        Program.discord.Logger.LogInformation(Program.BotEventId,
            "[PackageUpdateCheck] Finished checking for updates on all hosts.");
#endif

        if (restartRequired) restartRequiredResponse = "A system restart is required to complete package updates.";

        if (updatesAvailable || restartRequired)
        {
            if (updatesAvailable)
                updatesAvailableResponse = "Package updates are available on the following hosts:\n" +
                                           updatesAvailableResponse;

            var ownerMention = "";
            foreach (var user in Program.discord.CurrentApplication.Owners) ownerMention += user.Mention + " ";

            var response = updatesAvailableResponse + restartRequiredResponse;
            await Program.homeChannel.SendMessageAsync($"{ownerMention.Trim()}\n{response}");
        }
    }

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
                var slashCommands = await Program.discord.GetGuildApplicationCommandsAsync(Program.configjson.HomeServerId);
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
                    var msg = await targetChannel.SendMessageAsync($"<@{reminderData.UserId}>, I have a reminder for you:",
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

                        ulong guildId = Convert.ToUInt64(reminderData.GuildId);
                        DiscordGuild guild = await Program.discord.GetGuildAsync(guildId);
                        DiscordMember targetMember = await guild.GetMemberAsync(reminderData.UserId);

                        var msg = await targetMember.SendMessageAsync($"<@{reminderData.UserId}>, I have a reminder for you:",
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

    public class PerServerFeatures
    {
        public static async Task WednesdayCheck()
        {
#if DEBUG
            Program.discord.Logger.LogInformation(Program.BotEventId, "WednesdayCheck running.");
#endif
            if (DateTime.Now.DayOfWeek != DayOfWeek.Wednesday)
                return;
            if (!DateTime.Now.ToShortTimeString().Contains("10:00")) return;

            try
            {
                var channel = await Program.discord.GetChannelAsync(874488354786394192);
            }
            catch (Exception e)
            {
                Program.discord.Logger.LogError(Program.BotEventId, "An error occurred! Details: {e}", e);
            }
        }
    }
}
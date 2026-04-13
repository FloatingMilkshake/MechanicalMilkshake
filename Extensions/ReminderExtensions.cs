namespace MechanicalMilkshake.Extensions;

internal static class ReminderExtensions
{
    extension(IEnumerable<Setup.Types.Reminder> reminders)
    {
        internal async Task<DiscordEmbed> CreateEmbedAsync()
        {
            string embedDescription = "";
            foreach (var reminder in reminders)
            {
                var reminderSetTimeTimestamp = reminder.GetSetTimeTimestamp();
                var reminderTriggerTimeTimestamp = reminder.GetTriggerTimeTimestamp();

                string guildName;
                if (Setup.Constants.RegularExpressions.DiscordIdPattern.IsMatch(reminder.GuildId))
                {
                    var targetGuild = Setup.State.Discord.Client.Guilds[Convert.ToUInt64(reminder.GuildId)];
                    guildName = targetGuild.Name;
                }
                else
                {
                    guildName = "DMs";
                }

                var reminderText = reminder.ReminderText.Truncate(350, " *(truncated)*");

                if (guildName != "DMs")
                    guildName += $" <#{reminder.ChannelId}>";

                embedDescription += $"`{reminder.ReminderId}`:\n"
                          + (string.IsNullOrWhiteSpace(reminderText)
                              ? ""
                              : $"> {reminderText}\n")
                          + $"[Set <t:{reminderSetTimeTimestamp}:R>]({reminder.GetJumpLink()}) to remind you <t:{reminderTriggerTimeTimestamp}:R> in {guildName}\n\n";
            }

            DiscordEmbedBuilder embed = new()
            {
                Title = "Reminders",
                Color = Setup.Constants.BotColor
            };

            if (embedDescription.Length > 4096)
            {
                embed.WithColor(DiscordColor.Red);

                var embedDescriptionWithTruncatedReminders = $"You have too many reminders to list here! Here are the IDs of each one. Use {"reminder show".AsSlashCommandMention()} for details.\n";

                foreach (var reminder in reminders)
                {
                    var reminderSetTimeTimestamp = reminder.GetSetTimeTimestamp();
                    long reminderTriggerTimeTimestamp = reminder.GetTriggerTimeTimestamp();
                    embedDescriptionWithTruncatedReminders += $"\n`{reminder.ReminderId}` - set <t:{reminderSetTimeTimestamp}:R> to remind you <t:{reminderTriggerTimeTimestamp}:R>";
                }

                embed.WithDescription(embedDescriptionWithTruncatedReminders);
            }
            else
            {
                embed.WithDescription(embedDescription);
            }

            return embed;
        }

        internal DiscordSelectComponent CreateSelectComponent(string componentCustomId)
        {
            List<DiscordSelectComponentOption> options = reminders.Select(reminder =>
                new DiscordSelectComponentOption(string.IsNullOrWhiteSpace(reminder.ReminderText)
                    ? "[no content]"
                    : reminder.ReminderText.Truncate(100),
                    reminder.ReminderId.ToString(),
                    reminder.TriggerTime.Humanize()))
                .ToList();

            return new DiscordSelectComponent(componentCustomId, null, options);
        }
    }
}

namespace MechanicalMilkshake.Helpers;

public class ReminderHelpers
{
    public static async Task LogReminderErrorAsync(DiscordChannel logChannel, Exception ex)
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

        Program.Discord.Logger.LogError(Program.BotEventId, "An exception occurred when checking reminders!"
            + "\n{exType}: {exMessage}\n{exStackTrace}", ex.GetType(), ex.Message, ex.StackTrace);

        await logChannel.SendMessageAsync(errorEmbed);
    }

    public static void AddReminderPushbackEmbedField(DiscordEmbedBuilder embed, ulong msgId = default)
    {
        var id = msgId == default ? "[loading...]" : $"`{msgId}`";

        embed.AddField("Need to delay this reminder?",
            $"Use {SlashCmdMentionHelpers.GetSlashCmdMention("reminder", "pushback")} and set `message` to {id}.");
    }
}
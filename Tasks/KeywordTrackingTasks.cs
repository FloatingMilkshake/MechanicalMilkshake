namespace MechanicalMilkshake.Tasks;

internal static class KeywordTrackingTasks
{
    internal static async Task ExecuteAsync()
    {
        while (true)
        {
            await CheckPendingMessagesAsync();
            await Task.Delay(TimeSpan.FromSeconds(10));
        }
    }

    private static async Task CheckPendingMessagesAsync()
    {
        var count = MessageEvents.MessagesPendingKeywordCheck.Count();
        if (count > 0)
        {
            Setup.State.Discord.Client.Logger.LogDebug("Processing {count} messages pending keyword check", count);
            try
            {
                Dictionary<DateTime, (DiscordMessage message, bool isEditEvent)> snapshot;
                lock (MessageEvents.MessagesPendingKeywordCheckLock) snapshot = new(MessageEvents.MessagesPendingKeywordCheck);
                int checkedMessages = 0;
                foreach (var pendingMessage in snapshot)
                {
                    checkedMessages++;
                    Setup.State.Discord.Client.Logger.LogDebug("Checking message {message} in channel {channel} for tracked keywords ({checkedCount}/{total})", pendingMessage.Value.message.Id, pendingMessage.Value.message.Channel.Id, checkedMessages, count);
                    await pendingMessage.Value.message.CheckForTrackedKeywordsAsync(pendingMessage.Value.isEditEvent);
                    MessageEvents.MessagesPendingKeywordCheck.Remove(pendingMessage.Key);
                    Setup.State.Process.MessagesCheckedForTrackedKeywords++;
                    await Task.Delay(TimeSpan.FromSeconds(1));
                }
            }
            catch (Exception ex)
            {
                Setup.State.Discord.Client.Logger.LogError(ex, $"Failed to process messages pending keyword check! Will retry on next task run");
            }
            Setup.State.Discord.Client.Logger.LogDebug($"Done processing messages pending keyword check");
        }
    }
}

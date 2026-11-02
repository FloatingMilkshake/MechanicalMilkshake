namespace MechanicalMilkshake.Tasks;

internal static class KeywordTrackingTasks
{
    internal static async Task ExecuteAsync()
    {
        // Restore state at startup
        var pendingKeywordChecks = await Setup.Storage.Redis.HashGetAllAsync("pendingKeywordChecks");
        Setup.State.Discord.Client.Logger.LogDebug("Restoring state for keyword tracking, found {count} entries", pendingKeywordChecks.Length);
        foreach (var pkc in pendingKeywordChecks)
        {
            try
            {
                var timestamp = new DateTime(long.Parse(pkc.Name.ToString()));
                var value = pkc.Value.ToString().Split(":");
                var channelId = ulong.Parse(value[0]);
                var msgId = ulong.Parse(value[1]);
                var isEdit = bool.Parse(value[2]);

                var channel = await Setup.State.Discord.Client.GetChannelAsync(channelId);
                var message = await channel.GetMessageAsync(msgId);

                lock (MessageEvents.MessagesPendingKeywordCheckLock) MessageEvents.MessagesPendingKeywordCheck[timestamp] = (message, isEdit);
            }
            catch (Exception ex) when (ex is NotFoundException or UnauthorizedException)
            {
                await Setup.Storage.Redis.HashDeleteAsync("pendingKeywordChecks", pkc.Name);
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
        Setup.State.Discord.Client.Logger.LogDebug("Successfully restored state for keyword tracking, starting task");

        while (true)
        {
            await CheckPendingMessagesAsync();
#if DEBUG
            await Task.Delay(TimeSpan.FromSeconds(10));
#else
            await Task.Delay(TimeSpan.FromMinutes(1));
#endif
        }
    }

    private static async Task CheckPendingMessagesAsync()
    {
        var count = MessageEvents.MessagesPendingKeywordCheck.Count();
        if (count > 0)
        {
            Setup.State.Discord.Client.Logger.LogDebug("Processing {count} messages pending keyword check", count);
            Dictionary<DateTime, (DiscordMessage message, bool isEditEvent)> snapshot;
            lock (MessageEvents.MessagesPendingKeywordCheckLock) snapshot = new(MessageEvents.MessagesPendingKeywordCheck);
            int checkedMessages = 0;
            foreach (var pendingMessage in snapshot)
            {
                checkedMessages++;
                Setup.State.Discord.Client.Logger.LogDebug("Checking message {message} in channel {channel} for tracked keywords ({checkedCount}/{total})", pendingMessage.Value.message.Id, pendingMessage.Value.message.Channel.Id, checkedMessages, count);
                try
                {
                    await pendingMessage.Value.message.CheckForTrackedKeywordsAsync(pendingMessage.Value.isEditEvent);
                    MessageEvents.MessagesPendingKeywordCheck.Remove(pendingMessage.Key);
                    await Setup.Storage.Redis.HashDeleteAsync("pendingKeywordChecks", pendingMessage.Key.Ticks);
                    Setup.State.Process.MessagesCheckedForTrackedKeywords++;
                }
                catch (Exception ex)
                {
                    Setup.State.Discord.Client.Logger.LogWarning(ex, "Failed to check message {message} in channel {channel} for tracked keywords!", pendingMessage.Value.message.Id, pendingMessage.Value.message.Channel.Id);
                    if (ex is NotFoundException or UnauthorizedException)
                    {
                        // The message couldn't be checked, potentially because of bad permissions.
                        // Skip it and carry on.
                        MessageEvents.MessagesPendingKeywordCheck.Remove(pendingMessage.Key);
                        await Setup.Storage.Redis.HashDeleteAsync("pendingKeywordChecks", pendingMessage.Key.Ticks);
                        await LogKeywordCheckErrorAsync(ex, pendingMessage.Value.message, false);
                    }
                    else
                    {
                        // The message couldn't be checked, this was probably a transient error.
                        // Try again next run.
                        await LogKeywordCheckErrorAsync(ex, pendingMessage.Value.message, true);
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(1));
            }
            Setup.State.Discord.Client.Logger.LogDebug($"Done processing messages pending keyword check");
        }
    }

    private static async Task LogKeywordCheckErrorAsync(Exception ex, DiscordMessage message, bool willRetry)
    {
        var embed = new DiscordEmbedBuilder()
        {
            Title = "An exception occurred while processing tracked keywords",
            Color = DiscordColor.Red,
            Description = $"An exception occurred when processing message {message.Id} in channel {message.Channel.Id}."
                + $"\n```\n{ex.GetType()}: {ex.Message}\n```",
        }.AddField("Will retry?", willRetry ? "Yes" : "No");
        await Setup.State.Discord.Channels.Home.SendMessageAsync(embed);
    }
}

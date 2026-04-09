namespace MechanicalMilkshake.Events;

internal class HeartbeatEvent
{
    internal static async Task HandleHeartbeatEventAsync(IGatewayClient client)
    {
        if (Setup.Configuration.ConfigJson.UptimeKumaHeartbeatUrl is null or "") return;

        try
        {
            var heartbeatResponse = await Setup.Constants.HttpClient.GetAsync($"{Setup.Configuration.ConfigJson.UptimeKumaHeartbeatUrl}{client.Ping.TotalMilliseconds}");
            if (heartbeatResponse.IsSuccessStatusCode && Setup.State.Discord.Client.Logger.IsEnabled(LogLevel.Debug))
                Setup.State.Discord.Client.Logger.LogDebug("Successfully sent Uptime Kuma heartbeat with ping {ping}ms", client.Ping.TotalMilliseconds);
            else if (!heartbeatResponse.IsSuccessStatusCode)
                Setup.State.Discord.Client.Logger.LogWarning("Uptime Kuma heartbeat failed with status code {statusCode}", heartbeatResponse.StatusCode);
            Setup.State.Process.LastUptimeKumaHeartbeatStatus = heartbeatResponse.StatusCode.ToString();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException hrex)
                Setup.State.Discord.Client.Logger.LogWarning("Uptime Kuma heartbeat failed with status code {statusCode}: {exType}: {exMessage}\n{stackTrace}", hrex.StatusCode, hrex.GetType(), hrex.Message, hrex.StackTrace);
            else
                Setup.State.Discord.Client.Logger.LogWarning("Uptime Kuma heartbeat failed: {exType}: {exMessage}\n{stackTrace}", ex.GetType(), ex.Message, ex.StackTrace);

            Setup.State.Process.LastUptimeKumaHeartbeatStatus = "failed";
        }
    }
}

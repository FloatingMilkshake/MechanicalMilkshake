namespace MechanicalMilkshake.Tasks;

internal class HeartbeatTasks
{
    internal static async Task ExecuteAsync()
    {
        if (Setup.State.Process.Configuration.UptimeKumaHeartbeatUrl is null or "") return;

        while (true)
        {
            await HeartbeatAsync();
            await Task.Delay(60000);
        }
    }

    private static async Task HeartbeatAsync()
    {
        try
        {
            if (!Setup.State.Discord.Client.AllShardsConnected)
                return;

            var ping = Setup.State.Discord.Client.GetConnectionLatency(0).Milliseconds;
            var heartbeatResponse = await Setup.Constants.HttpClient.GetAsync($"{Setup.State.Process.Configuration.UptimeKumaHeartbeatUrl}{ping}");
            if (heartbeatResponse.IsSuccessStatusCode && Setup.State.Discord.Client.Logger.IsEnabled(LogLevel.Debug))
                Setup.State.Discord.Client.Logger.LogDebug("Successfully sent Uptime Kuma heartbeat with ping {ping}ms", ping);
            else if (!heartbeatResponse.IsSuccessStatusCode)
                Setup.State.Discord.Client.Logger.LogWarning("Uptime Kuma heartbeat failed with status code {statusCode}", heartbeatResponse.StatusCode);
            Setup.State.Process.LastUptimeKumaHeartbeatStatus = heartbeatResponse.StatusCode.ToString();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException hrex)
                Setup.State.Discord.Client.Logger.LogWarning(ex, "Uptime Kuma heartbeat failed with status code {statusCode}:", hrex.StatusCode);
            else
                Setup.State.Discord.Client.Logger.LogWarning(ex, "Uptime Kuma heartbeat failed:");

            Setup.State.Process.LastUptimeKumaHeartbeatStatus = "failed";
        }
    }
}

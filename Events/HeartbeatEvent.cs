namespace MechanicalMilkshake.Events;

public class HeartbeatEvent
{
    public static async Task Heartbeated(DiscordClient client, HeartbeatEventArgs e)
    {
        if (Program.ConfigJson.Base.UptimeKumaHeartbeatUrl is null or "") return;
        
        try
        {
            var heartbeatResponse = await Program.HttpClient.GetAsync($"{Program.ConfigJson.Base.UptimeKumaHeartbeatUrl}{e.Ping}");
            if (heartbeatResponse.IsSuccessStatusCode)
                Program.Discord.Logger.LogDebug(Program.BotEventId, "Successfully sent Uptime Kuma heartbeat with ping {ping}ms", e.Ping);
            else
                Program.Discord.Logger.LogWarning(Program.BotEventId, "Uptime Kuma heartbeat failed with status code {statusCode}", heartbeatResponse.StatusCode);
        }
        catch (Exception ex)
        {
            Program.Discord.Logger.LogWarning(Program.BotEventId, "Uptime Kuma heartbeat failed: {exType}: {exMessage}\n{stackTrace}", ex.GetType(), ex.Message, ex.StackTrace);
        }
    }
}
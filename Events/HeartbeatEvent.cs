using DSharpPlus.Net.Gateway;

namespace MechanicalMilkshake.Events;

public class HeartbeatEvent
{
    public static async Task Heartbeated(IGatewayClient client)
    {
        if (Program.ConfigJson.UptimeKumaHeartbeatUrl is null or "") return;
        
        try
        {
            var heartbeatResponse = await Program.HttpClient.GetAsync($"{Program.ConfigJson.UptimeKumaHeartbeatUrl}{client.Ping.TotalMilliseconds}");
            if (heartbeatResponse.IsSuccessStatusCode)
                Program.Discord.Logger.LogDebug(Program.BotEventId, "Successfully sent Uptime Kuma heartbeat with ping {ping}ms", client.Ping.TotalMilliseconds);
            else
                Program.Discord.Logger.LogWarning(Program.BotEventId, "Uptime Kuma heartbeat failed with status code {statusCode}", heartbeatResponse.StatusCode);
            Program.LastUptimeKumaHeartbeatStatus = heartbeatResponse.StatusCode.ToString();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException hrex)
                Program.Discord.Logger.LogWarning(Program.BotEventId, "Uptime Kuma heartbeat failed with status code {statusCode}: {exType}: {exMessage}\n{stackTrace}", hrex.StatusCode, hrex.GetType(), hrex.Message, hrex.StackTrace);
            else
                Program.Discord.Logger.LogWarning(Program.BotEventId, "Uptime Kuma heartbeat failed: {exType}: {exMessage}\n{stackTrace}", ex.GetType(), ex.Message, ex.StackTrace);
            
            Program.LastUptimeKumaHeartbeatStatus = "failed";
        }
    }
}
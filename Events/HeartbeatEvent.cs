using DSharpPlus.Net.Gateway;

namespace MechanicalMilkshake.Events;

public class HeartbeatEvent
{
    public static async Task Heartbeated(IGatewayClient client)
    {
        if (Program.ConfigJson.Base.UptimeKumaHeartbeatUrl is null or "") return;
        
        try
        {
            var heartbeatResponse = await Program.HttpClient.GetAsync($"{Program.ConfigJson.Base.UptimeKumaHeartbeatUrl}{client.Ping}");
            if (heartbeatResponse.IsSuccessStatusCode)
                Program.Discord.Logger.LogDebug(Program.BotEventId, "Successfully sent Uptime Kuma heartbeat with ping {ping}ms", client.Ping);
            else
                Program.Discord.Logger.LogWarning(Program.BotEventId, "Uptime Kuma heartbeat failed with status code {statusCode}", heartbeatResponse.StatusCode);
            Program.LastUptimeKumaHeartbeatStatus = heartbeatResponse.StatusCode.ToString();
        }
        catch (Exception ex)
        {
            Program.Discord.Logger.LogWarning(Program.BotEventId, "Uptime Kuma heartbeat failed: {exType}: {exMessage}\n{stackTrace}", ex.GetType(), ex.Message, ex.StackTrace);
            Program.LastUptimeKumaHeartbeatStatus = "exception thrown";
        }
    }
}
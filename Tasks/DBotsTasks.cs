namespace MechanicalMilkshake.Tasks;

public class DBotsTasks
{
    public static async Task ExecuteAsync()
    {
        if (Program.ConfigJson.DBots is null ||
            string.IsNullOrWhiteSpace(Program.ConfigJson.DBots.ApiToken) ||
            string.IsNullOrWhiteSpace(Program.ConfigJson.DBots.ApiEndpoint))
        {
            Program.Discord.Logger.LogWarning(Program.BotEventId, "DBots stats posting disabled due to missing configuration.");
            return;
        }

        Program.Discord.Logger.LogDebug(Program.BotEventId, "DBots stats posting {Status}.",
            Program.ConfigJson.DBots.DoStatsPosting ? "enabled" : "disabled");
        
        while (true)
        {
            await UpdateStatsAsync();
            await Task.Delay(TimeSpan.FromHours(1));
        }
        // ReSharper disable once FunctionNeverReturns
    }
    
    public static async Task UpdateStatsAsync()
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, Program.ConfigJson.DBots.ApiEndpoint);
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Program.ConfigJson.DBots.ApiToken);
        requestMessage.Content = JsonContent.Create(new { guildCount = Program.Discord.Guilds.Count });
        
        using var response = await Program.HttpClient.SendAsync(requestMessage);
        
        // Debug logs
        if (response.IsSuccessStatusCode)
        {
            Program.Discord.Logger.LogDebug(Program.BotEventId, "Successfully posted stats to DBots.");
        }
        else
        {
            Program.Discord.Logger.LogError(Program.BotEventId, "Failed posting stats to DBots. {StatusCode} {Reason}",
                response.StatusCode, response.ReasonPhrase);
        }
    }
}
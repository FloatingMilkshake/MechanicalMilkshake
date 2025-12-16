namespace MechanicalMilkshake.Tasks;

public class DBotsTasks
{
    public static async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(Program.ConfigJson.DbotsApiToken))
        {
            Program.Discord.Logger.LogWarning(Program.BotEventId, "DBots stats posting disabled due to missing configuration.");
            return;
        }

        Program.Discord.Logger.LogDebug(Program.BotEventId, "DBots stats posting {Status}.",
            Program.ConfigJson.DoDbotsStatsPosting ? "enabled" : "disabled");
        
        if (!Program.ConfigJson.DoDbotsStatsPosting)
            return;
        
        while (true)
        {
            await UpdateStatsAsync();
            await Task.Delay(TimeSpan.FromHours(1));
        }
    }
    
    public static async Task UpdateStatsAsync()
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://discord.bots.gg/api/v1/bots/{Program.Discord.CurrentUser.Id}/stats");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Program.ConfigJson.DbotsApiToken);
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
                (int)response.StatusCode, response.ReasonPhrase);
        }
    }
}
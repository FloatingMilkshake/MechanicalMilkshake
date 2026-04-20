namespace MechanicalMilkshake.Tasks;

internal class DBotsTasks
{
    internal static async Task ExecuteAsync()
    {
        if (string.IsNullOrWhiteSpace(Setup.State.Process.Configuration.DbotsApiToken))
            return;

        if (!Setup.State.Process.Configuration.DoDbotsStatsPosting)
            return;

        while (true)
        {
            await UpdateStatsAsync();
            await Task.Delay(TimeSpan.FromHours(24));
        }
    }

    private static async Task UpdateStatsAsync()
    {
        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"https://discord.bots.gg/api/v1/bots/{Setup.State.Discord.Client.CurrentUser.Id}/stats");
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue(Setup.State.Process.Configuration.DbotsApiToken);
        requestMessage.Content = JsonContent.Create(new { guildCount = Setup.State.Discord.Client.Guilds.Count });

        using var response = await Setup.Constants.HttpClient.SendAsync(requestMessage);

        // Debug logs
        if (response.IsSuccessStatusCode)
        {
            Setup.State.Discord.Client.Logger.LogDebug("Successfully posted stats to DBots.");
        }
        else
        {
            Setup.State.Discord.Client.Logger.LogError("Failed posting stats to DBots. {StatusCode} {Reason}",
                (int)response.StatusCode, response.ReasonPhrase);
        }
    }
}

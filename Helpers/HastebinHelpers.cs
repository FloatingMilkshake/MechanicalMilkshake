using System.Net;

namespace MechanicalMilkshake.Helpers;

public class HastebinHelpers
{
    public static async Task<string> UploadToHastebinAsync(string text)
    {
        if (Program.ConfigJson.Base.HastebinUrl is null) return "Failed to upload; Hastebin URL missing from config.json";
        
        var client = Program.HttpClient;
        var response =
            await client.PostAsync($"{Program.ConfigJson.Base.HastebinUrl}/documents", new StringContent(text));
        
        if (response.StatusCode != HttpStatusCode.OK)
            return $"[Upload failed. Hastebin returned status code `{(int)response.StatusCode} {response.StatusCode}`.]";
        
        var responseContent = await response.Content.ReadAsStringAsync();
        var responseJson = JObject.Parse(responseContent);
        return $"{Program.ConfigJson.Base.HastebinUrl}/{responseJson["key"]}";
    }
}
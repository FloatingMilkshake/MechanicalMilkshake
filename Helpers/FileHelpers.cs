namespace MechanicalMilkshake.Helpers;

public class FileHelpers
{
    public static string ReadFile(string fileNameAndExtension, string fallback = "")
    {
        return File.Exists("CommitHash.txt")
            ? new StreamReader("CommitHash.txt").ReadToEnd().Trim()
            : fallback;
    }

    public static async Task<string> ReadFileAsync(string fileNameAndExtension, string fallback = "")
    {
        return File.Exists("CommitHash.txt")
            ? (await new StreamReader("CommitHash.txt").ReadToEndAsync()).Trim()
            : fallback;
    }
}
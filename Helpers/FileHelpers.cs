namespace MechanicalMilkshake.Helpers;

public class FileHelpers
{
    public static string ReadFile(string fileNameAndExtension, string fallback = "")
    {
        return File.Exists(fileNameAndExtension)
            ? new StreamReader(fileNameAndExtension).ReadToEnd().Trim()
            : fallback;
    }

    public static async Task<string> ReadFileAsync(string fileNameAndExtension, string fallback = "")
    {
        return File.Exists(fileNameAndExtension)
            ? (await new StreamReader(fileNameAndExtension).ReadToEndAsync()).Trim()
            : fallback;
    }
}
namespace MechanicalMilkshake.Helpers;

internal class FileHelpers
{
    internal static async Task<string> ReadFileAsync(string fileNameAndExtension, string fallback = "")
    {
        return File.Exists(fileNameAndExtension)
            ? (await File.ReadAllTextAsync(fileNameAndExtension)).Trim()
            : fallback;
    }
}

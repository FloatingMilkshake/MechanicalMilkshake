namespace MechanicalMilkshake.Extensions;

internal static class FileExtensions
{
    extension(File)
    {
        internal static async Task<string> ReadAllTextOrFallbackAsync(string fileNameAndExtension, string fallback = "")
        {
            return File.Exists(fileNameAndExtension)
                ? (await File.ReadAllTextAsync(fileNameAndExtension)).Trim()
                : fallback;
        }
    }
}

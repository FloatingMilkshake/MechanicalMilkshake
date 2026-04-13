namespace MechanicalMilkshake.Extensions;

internal static class DiscordMessageBuilderExtensions
{
    extension(DiscordMessageBuilder discordMessageBuilder)
    {
        internal int GetTotalEmbedTextContentLength()
        {
            int length = 0;
            foreach (var embed in discordMessageBuilder.Embeds)
            {
                length += embed.Title?.Length ?? 0;
                length += embed.Description?.Length ?? 0;
                length += embed.Footer?.Text?.Length ?? 0;
                length += embed.Author?.Name?.Length ?? 0;
                foreach (var field in embed.Fields)
                {
                    length += field.Name?.Length ?? 0;
                    length += field.Value?.Length ?? 0;
                }
            }
            return length;
        }
    }
}

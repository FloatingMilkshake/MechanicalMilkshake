namespace MechanicalMilkshake.Extensions;

internal static class DiscordGuildExtensions
{
    extension(DiscordGuild guild)
    {
        internal async Task<DiscordMessageBuilder> CreateInfoMessageAsync()
        {
            var message = new DiscordMessageBuilder().WithContent($"Server Info for **{guild.Name}**");

            var description = "None";
            if (guild.Description is not null)
                description = guild.Description;

            var createdAt = $"{guild.Id.ToUnixTimeSeconds()}";

            var categoryCount = guild.Channels.Count(channel => channel.Value.Type == DiscordChannelType.Category);

            message.AddEmbed(new DiscordEmbedBuilder()
                .WithColor(Setup.Constants.BotColor)
                .AddField("Server Owner", $"{(await guild.GetGuildOwnerAsync()).GetFullUsername()}")
                .AddField("Description", $"{description}")
                .AddField("Created on", $"<t:{createdAt}:F> (<t:{createdAt}:R>)")
                .AddField("Channels", $"{guild.Channels.Count - categoryCount}", true)
                .AddField("Categories", $"{categoryCount}", true)
                .AddField("Roles", $"{guild.Roles.Count}", true)
                .AddField("Members", $"{guild.MemberCount}", true)
                .WithThumbnail($"{guild.IconUrl}")
                .WithFooter($"Server ID: {guild.Id}"));

            return message;
        }
    }
}

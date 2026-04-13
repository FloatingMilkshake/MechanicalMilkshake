namespace MechanicalMilkshake.Commands;

[Command("emoji")]
[Description("Commands for working with emoji.")]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
internal class EmojiCommands
{
    [Command("link")]
    [Description("Get the link for an emoji! Only works for custom emoji.")]
    public static async Task EmojiLinkCommandAsync(SlashCommandContext ctx, [Parameter("emoji"), Description("The emoji to get the link for. Accepts one or more custom emoji.")] string emoji)
    {
        await ctx.DeferResponseAsync();

        if (!Setup.Constants.RegularExpressions.EmojiPattern.IsMatch(emoji))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("That doesn't look like an emoji! Please try again."));
            return;
        }

        var matches = Setup.Constants.RegularExpressions.EmojiPattern.Matches(emoji);

        var response = "";
        foreach (var match in matches.ToList())
        {
            var groups = match.Groups;
            var emojiUrl = groups[1].Value == "a"
            ? $"https://cdn.discordapp.com/emojis/{groups[3].Value}.gif"
            : $"https://cdn.discordapp.com/emojis/{groups[3].Value}.png";
            response += $"{emojiUrl}\n";
        }

        if (response.Length > 4000)
            response = "It looks like this message is too long to send! Try entering less emoji.";

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(response));
    }
}

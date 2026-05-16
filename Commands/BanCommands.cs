namespace MechanicalMilkshake.Commands;

[RequirePermissions(DiscordPermission.BanMembers)]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal class BanCommands
{
    [Command("ban")]
    [Description("Ban a user. They will not be able to rejoin unless unbanned.")]
    public static async Task BanCommandAsync(SlashCommandContext ctx,
        [Parameter("user"), Description("The user to ban.")] DiscordUser userToBan,
        [Parameter("reason"), Description("The reason for the ban.")] [MinMaxLength(maxLength: 1500)]
        string reason = "No reason provided.",
        [SlashChoiceProvider(typeof(BanDeleteMessagesChoiceProvider))]
        [Parameter("delete_messages"), Description("How much of the user's message history to delete. Default: Keep Messages")]
        string deleteMessages = "0")
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true));

        DiscordMember targetMember = default;
        try
        {
            targetMember = await ctx.Guild.GetMemberAsync(userToBan.Id);
        }
        catch (NotFoundException)
        {
            // not in server
        }

        if (!ctx.Member.CanModerate(targetMember))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have permission to ban **{userToBan.GetFullUsername()}**!")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            return;
        }

        if (!ctx.Guild.CurrentMember.CanModerate(targetMember))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"I don't have permission to ban **{userToBan.GetFullUsername()}**! Please check the role hierarchy and permissions.")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            return;
        }

        bool isUserAlreadyBanned = false;
        try
        {
            var ban = await ctx.Guild.GetBanAsync(userToBan);
            if (ban != default)
                isUserAlreadyBanned = true;
        }
        catch (NotFoundException)
        {
            // User isn't banned, handled below
        }

        if (isUserAlreadyBanned)
        {
            // Unban first to update the reason
            // I hate doing this but Discord doesn't update the reason if we just try to ban again without unbanning first
            await ctx.Guild.UnbanMemberAsync(userToBan, "Unbanning to ban with new reason");
        }

        TimeSpan msgDeleteTimeSpan = deleteMessages switch
        {
            "0" => TimeSpan.Zero,
            "1h" => TimeSpan.FromHours(1),
            "6h" => TimeSpan.FromHours(6),
            "12h" => TimeSpan.FromHours(12),
            "24h" => TimeSpan.FromHours(24),
            "3d" => TimeSpan.FromDays(3),
            "7d" => TimeSpan.FromDays(7),
            _ => TimeSpan.Zero // this should never happen because we use a choice provider, but whatever
        };

        await ctx.Guild.BanMemberAsync(userToBan.Id, msgDeleteTimeSpan, $"Banned by {ctx.User.Username}: {reason}");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User banned successfully.")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));

        await ctx.Channel.SendMessageAsync($"{userToBan.Mention} has been banned: **{reason}**");
    }

    [Command("unban")]
    [Description("Unban a user.")]
    public static async Task UnbanCommandAsync(SlashCommandContext ctx,
        [Parameter("user"), Description("The user to unban.")] DiscordUser userToUnban)
    {
        await ctx.DeferResponseAsync(true);

        try
        {
            await ctx.Guild.UnbanMemberAsync(userToUnban, $"Unbanned by {ctx.User.Username}");
        }
        catch (NotFoundException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"**{userToUnban.GetFullUsername()}** isn't banned!")
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User unbanned successfully.")
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(true)));
        await ctx.Channel.SendMessageAsync(
            $"Successfully unbanned **{userToUnban.GetFullUsername()}**!");
    }

    private class BanDeleteMessagesChoiceProvider : IChoiceProvider
    {
        private static readonly IReadOnlyList<DiscordApplicationCommandOptionChoice> Choices =
        [
            new("Keep Messages", "0"),
            new("1 hour", "1h"),
            new("6 hours", "6h"),
            new("12 hours", "12h"),
            new("24 hours", "24h"),
            new("3 days", "3d"),
            new("7 days", "7d")
        ];

        public async ValueTask<IEnumerable<DiscordApplicationCommandOptionChoice>> ProvideAsync(CommandParameter parameter) => Choices;
    }
}

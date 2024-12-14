namespace MechanicalMilkshake.Commands;

public partial class InviteInfo
{
    [Command("inviteinfo")]
    [Description("Return information about a Discord invite.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts(DiscordInteractionContextType.Guild, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.BotDM)]
    public static async Task InviteInfoCommand(MechanicalMilkshake.SlashCommandContext ctx,
        [Parameter("invite"), Description("The invite to return information about. Accepts an invite link or code.")]
        string targetInvite)
    {
        await ctx.DeferResponseAsync();

        if (targetInvite.Contains(".gg")) // discord.gg link
            targetInvite = DiscordDotGgLinkPattern().Replace(targetInvite, "");
        else if (targetInvite.Contains("discord.com") ||
                 targetInvite.Contains("discordapp.com")) // discord(app).com/invite link
            targetInvite = DiscordDotComLinkPattern().Replace(targetInvite, "");

        DiscordInvite invite;
        try
        {
            invite = await Program.Discord.GetInviteByCodeAsync(targetInvite, true, true);
        }
        catch
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("That's not a valid invite!"));
            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = invite.Guild.VanityUrlCode is null
                ? $"Invite Info for {invite.Guild.Name}"
                : $"Invite info for {invite.Guild.Name}\n(discord.gg/{invite.Guild.VanityUrlCode})",
            //Title = $"Invite Info for {invite.Guild.Name}",
            Description = invite.Guild.Description,
            Color = Program.BotColor
        };

        if (invite.Guild.VanityUrlCode is null || invite.Code != invite.Guild.VanityUrlCode)
        {
            embed.AddField("Inviter",
                invite.Inviter is null
                    ? "unknown"
                    : $"{UserInfoHelpers.GetFullUsername(invite.Inviter)} (`{invite.Inviter.Id}`)");

            embed.AddField("Expires At",
                invite.ExpiresAt is null
                    ? "Invite does not expire."
                    : $"<t:{((DateTimeOffset)invite.ExpiresAt).ToUnixTimeSeconds()}:F> (<t:{((DateTimeOffset)invite.ExpiresAt).ToUnixTimeSeconds()}:R>)");
        }

        var verifLevelDesc = invite.Guild.VerificationLevel switch
        {
            DiscordVerificationLevel.None => "None - unrestricted access to the server",
            DiscordVerificationLevel.Low => "Low - members must have a verified email address on their Discord account",
            DiscordVerificationLevel.Medium => "Medium - members must be registered on Discord for longer than 5 minutes",
            DiscordVerificationLevel.High => "High - members must be a member of the server for longer than 10 minutes",
            DiscordVerificationLevel.Highest => "Highest - members must have a verified phone number on their Discord account",
            _ => "unknown"
        };
        embed.AddField("Verification Level", verifLevelDesc);

        embed.AddField("Server Created On",
            $"<t:{invite.Guild.CreationTimestamp.ToUnixTimeSeconds()}:F> (<t:{invite.Guild.CreationTimestamp.ToUnixTimeSeconds()}:R>)");

        embed.AddField("Members",
            invite.ApproximateMemberCount is null ? "unknown" : invite.ApproximateMemberCount.ToString(), true);

        embed.AddField("Online",
            invite.ApproximatePresenceCount is null ? "unknown" : invite.ApproximatePresenceCount.ToString(), true);

        embed.WithThumbnail(invite.Guild.IconUrl);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }

    [GeneratedRegex(".*.gg/")]
    private static partial Regex DiscordDotGgLinkPattern();
    [GeneratedRegex(@".*\/invite\/")]
    private static partial Regex DiscordDotComLinkPattern();
}
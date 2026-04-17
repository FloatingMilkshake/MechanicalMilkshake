namespace MechanicalMilkshake.Commands;

internal class InviteInfoCommands
{
    [Command("inviteinfo")]
    [Description("Return information about a Discord invite.")]
    [InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall, DiscordApplicationIntegrationType.UserInstall)]
    [InteractionAllowedContexts([DiscordInteractionContextType.BotDM, DiscordInteractionContextType.PrivateChannel, DiscordInteractionContextType.Guild])]
    public static async Task InviteInfoCommandAsync(SlashCommandContext ctx,
        [Parameter("invite"), Description("The invite to return information about. Accepts an invite link or code.")]
        string targetInvite)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(false));

        var inviteMatch = Setup.Constants.RegularExpressions.DiscordInvitePattern.Match(targetInvite);
        if (!inviteMatch.Success)
        {
            inviteMatch = Setup.Constants.RegularExpressions.DiscordInvitePattern.Match($"discord.gg/{targetInvite}");
            if (!inviteMatch.Success)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("That's not a valid invite!")
                    .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
                return;
            }
        }

        DiscordInvite invite;
        try
        {
            invite = await Setup.State.Discord.Client.GetInviteByCodeAsync(inviteMatch.Groups[1].Value, true);
        }
        catch (NotFoundException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("That's not a valid invite!")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = invite.Guild.VanityUrlCode is null
                ? $"Invite Info for {invite.Guild.Name}"
                : $"Invite info for {invite.Guild.Name}\n(discord.gg/{invite.Guild.VanityUrlCode})",
            //Title = $"Invite Info for {invite.Guild.Name}",
            Description = invite.Guild.Description,
            Color = Setup.Constants.BotColor
        };

        if (invite.Guild.VanityUrlCode is null || invite.Code != invite.Guild.VanityUrlCode)
        {
            embed.AddField("Inviter",
                invite.Inviter is null
                    ? "unknown"
                    : $"{invite.Inviter.GetFullUsername()} (`{invite.Inviter.Id}`)");

            embed.AddField("Expires At",
                invite.ExpiresAt is null
                    ? "Invite does not expire."
                    : $"<t:{invite.ExpiresAt.Value.ToUnixTimeSeconds()}:F> (<t:{invite.ExpiresAt.Value.ToUnixTimeSeconds()}:R>)");
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

        var guildCreationTimestamp = invite.Guild.CreationTimestamp.ToUnixTimeSeconds();
        embed.AddField("Server Created On", $"<t:{guildCreationTimestamp}:F> (<t:{guildCreationTimestamp}:R>)");

        embed.AddField("Members", invite.ApproximateMemberCount is null ? "unknown" : invite.ApproximateMemberCount.ToString(), true);

        embed.AddField("Online", invite.ApproximatePresenceCount is null ? "unknown" : invite.ApproximatePresenceCount.ToString(), true);

        embed.WithThumbnail(invite.Guild.IconUrl);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
    }
}

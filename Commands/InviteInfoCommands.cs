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
        await ctx.DeferResponseAsync(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false));

        var inviteMatch = Setup.Constants.RegularExpressions.DiscordInvitePattern.Match(targetInvite);
        if (!inviteMatch.Success)
        {
            inviteMatch = Setup.Constants.RegularExpressions.DiscordInvitePattern.Match($"discord.gg/{targetInvite}");
            if (!inviteMatch.Success)
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("That's not a valid invite!")
                    .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
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
                .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
            return;
        }

        if (invite.Guild is null)
        {
            // thanks Discord...

            if (invite.Code == "discord")
            {
                await ctx.FollowupAsync("???");
            }
            else
            {
                await ctx.FollowupAsync("Congratulations, you win! Where does this invite go? Seriously, please tell me. (Send me a DM!)");
            }

            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = $"Invite Info for {invite.Guild.Name}",
            Description = invite.Guild.Description,
            Color = Setup.Constants.BotColor
        };

        if (invite.Guild.VanityUrlCode is not null && invite.Code == invite.Guild.VanityUrlCode)
        {
            embed.AddField("Expires At", "This is a vanity invite. It never expires.");
        }
        else
        {
            if (invite.Guild.VanityUrlCode is not null)
            {
                embed.AddField("This server has a vanity invite!", $"discord.gg/{invite.Guild.VanityUrlCode}");
            }

            embed.AddField("Inviter",
                invite.Inviter is null
                    ? "unknown"
                    : $"{invite.Inviter.GetFullUsername()} (`{invite.Inviter.Id}`)");

            embed.AddField("Expires At",
                invite.ExpiresAt is null
                    ? "This invite does not expire."
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

        var guildFeatures = invite.Guild.Features;
        if (guildFeatures.Contains("DISCOVERABLE"))
        {
            embed.AddField("This server is Discoverable!", "It will appear in [Server Discovery](<https://discord.com/servers/discovery>).");
        }
        if (guildFeatures.Contains("MEMBER_VERIFICATION_MANUAL_APPROVAL"))
        {
            embed.AddField("This server has Apply to Join enabled!", "New members will need to submit an application and wait for approval before they can join the server.");
        }

        var guildCreationTimestamp = invite.Guild.CreationTimestamp.ToUnixTimeSeconds();
        embed.AddField("Server Created On", $"<t:{guildCreationTimestamp}:F> (<t:{guildCreationTimestamp}:R>)");

        embed.AddField("Members", invite.ApproximateMemberCount is null ? "unknown" : invite.ApproximateMemberCount.ToString(), true);

        embed.AddField("Online", invite.ApproximatePresenceCount is null ? "unknown" : invite.ApproximatePresenceCount.ToString(), true);

        embed.WithThumbnail(invite.Guild.IconUrl);

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .AddEmbed(embed)
            .AsEphemeral(ephemeral: ctx.Interaction.ShouldUseEphemeralResponse(false)));
    }
}

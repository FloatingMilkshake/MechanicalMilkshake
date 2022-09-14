using Emzi0767.Utilities;

namespace MechanicalMilkshake.Commands;

public class InviteInfo : ApplicationCommandModule
{
    [SlashCommand("inviteinfo", "Return information about a Discord invite.")]
    public async Task InviteInfoCommand(InteractionContext ctx,
        [Option("invite", "The invite to return information about. Accepts an invite link or code.")]
        string targetInvite)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

        if (targetInvite.Contains(".gg")) // discord.gg link
            targetInvite = Regex.Replace(targetInvite, ".*.gg/", "");
        else if (targetInvite.Contains("discord.com") ||
                 targetInvite.Contains("discordapp.com")) // discord(app).com/invite link
            targetInvite = Regex.Replace(targetInvite, @".*\/invite\/", "");

        DiscordInvite invite;
        try
        {
            invite = await Program.discord.GetInviteByCodeAsync(targetInvite, true, true);
        }
        catch
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("That's not a valid invite!"));
            return;
        }

        var embed = new DiscordEmbedBuilder
        {
            Title = invite.Guild.VanityUrlCode == null ? $"Invite Info for {invite.Guild.Name}" : $"Invite info for {invite.Guild.Name}\n(discord.gg/{invite.Guild.VanityUrlCode})",
            //Title = $"Invite Info for {invite.Guild.Name}",
            Description = invite.Guild.Description,
            Color = Program.botColor
        };
        
        if (invite.Guild.VanityUrlCode == null)
        {
            embed.AddField("Inviter",
                invite.Inviter == null
                    ? "unknown"
                    : $"{invite.Inviter.Username}#{invite.Inviter.Discriminator} (`{invite.Inviter.Id}`)");

            embed.AddField("Expires At",
                    invite.ExpiresAt == null
                        ? "Invite does not expire."
                        : $"<t:{((DateTimeOffset)invite.ExpiresAt).ToUnixTimeSeconds()}:F> (<t:{((DateTimeOffset)invite.ExpiresAt).ToUnixTimeSeconds()}:R>)");
        }

        string verifLevelDesc = invite.Guild.VerificationLevel switch
        {
            VerificationLevel.None => "None - unrestricted access to the server",
            VerificationLevel.Low => "Low - members must have a verified email address on their Discord account",
            VerificationLevel.Medium => "Medium - members must be registered on Discord for longer than 5 minutes",
            VerificationLevel.High => "High - members must be a member of the server for longer than 10 minutes",
            VerificationLevel.Highest => "Highest - members must have a verified phone number on their Discord account",
            _ => "unknown",
        };
        embed.AddField("Verification Level", verifLevelDesc);

        embed.AddField("Server Created On",
            $"<t:{invite.Guild.CreationTimestamp.ToUnixTimeSeconds()}:F> (<t:{invite.Guild.CreationTimestamp.ToUnixTimeSeconds()}:R>)");

        embed.AddField("Members",
            invite.ApproximateMemberCount == null ? "unknown" : invite.ApproximateMemberCount.ToString(), true);

        embed.AddField("Online",
            invite.ApproximatePresenceCount == null ? "unknown" : invite.ApproximatePresenceCount.ToString(), true);

        embed.WithThumbnail(invite.Guild.IconUrl);

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }
}
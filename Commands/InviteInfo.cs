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
            Title = $"Invite Info for {invite.Guild.Name}",
            Color = new DiscordColor("#FAA61A")
        };
        embed.AddField("Code", $"`{invite.Code}`");
        embed.AddField("Guild", $"{invite.Guild.Name} (`{invite.Guild.Id}`)", true);
        embed.AddField("Channel", $"{invite.Channel.Name} (`{invite.Channel.Id}`)", true);
        if (invite.Guild.VanityUrlCode == null)
        {
            embed.AddField("Inviter",
                invite.Inviter == null
                    ? "unknown"
                    : $"{invite.Inviter.Username}#{invite.Inviter.Discriminator} (`{invite.Inviter.Id}`)");
            embed.AddField("Approximate Member Count",
                invite.ApproximateMemberCount == null ? "unknown" : invite.ApproximateMemberCount.ToString(), true);
            embed.AddField("Expires At",
                invite.ExpiresAt == null
                    ? "Invite does not expire."
                    : $"<t:{((DateTimeOffset)invite.ExpiresAt).ToUnixTimeSeconds()}:F> (<t:{((DateTimeOffset)invite.ExpiresAt).ToUnixTimeSeconds()}:R>)");
        }
        else
        {
            embed.AddField("Approximate Member Count",
                invite.ApproximateMemberCount == null ? "unknown" : invite.ApproximateMemberCount.ToString());
        }

        embed.WithThumbnail(invite.Guild.IconUrl);

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
    }
}
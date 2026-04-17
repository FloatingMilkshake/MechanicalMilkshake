namespace MechanicalMilkshake.Commands;

[Command("timeout")]
[Description("Set or clear a timeout for a user.")]
[RequirePermissions(DiscordPermission.ModerateMembers)]
[InteractionInstallType(DiscordApplicationIntegrationType.GuildInstall)]
internal class TimeoutCommands
{
    [Command("set")]
    [Description("Time out a member.")]
    public static async Task TimeoutSetCommandAsync(SlashCommandContext ctx,
        [Parameter("member"), Description("The member to time out.")]
        DiscordUser user,
        [Parameter("duration"), Description("How long the timeout should last. Maximum value is 28 days due to Discord limitations.")]
        string duration,
        [Parameter("reason"), Description("The reason for the timeout.")]
        string reason = "No reason provided.")
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(true));

        DiscordMember targetMember;

        try
        {
            targetMember = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch (NotFoundException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"It doesn't look like **{user.Username}** is in the server, so I can't time them out!")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
            return;
        }

        if (!ctx.Member.CanModerate(targetMember))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have permission to time out **{user.GetFullUsername()}**!")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
            return;
        }

        if (targetMember.Permissions.HasPermission(DiscordPermission.Administrator))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"**{user.GetFullUsername()}** is an Administrator and can't be timed out!")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
            return;
        }

        TimeSpan parsedDuration;
        try
        {
            parsedDuration = HumanDateParser.HumanDateParser.Parse(duration)
                .Subtract(ctx.Interaction.CreationTimestamp.DateTime);
        }
        catch (HumanDateParser.ParseException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"I couldn't parse \"{duration}\" as a length of time! Please try again.")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
            return;
        }

        var expireTime = ctx.Interaction.CreationTimestamp.DateTime + parsedDuration;

        // This screwery lets users provide a duration of "28 days" without triggering a Bad Request response
        var latestAllowedTimeoutDate = DateTime.Now.AddDays(28);
        if (expireTime.AddSeconds(1) >= latestAllowedTimeoutDate)
        {
            if (expireTime < latestAllowedTimeoutDate)
            {
                expireTime = latestAllowedTimeoutDate.AddSeconds(-5);
            }
            else
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("Due to Discord limitations, timeouts can only be set for up to 28 days! Please set a shorter duration.")
                    .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
                return;
            }
        }

        if (!ctx.Guild.CurrentMember.CanModerate(targetMember))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"I don't have permission to time out **{user.GetFullUsername()}**! Please check the role hierarchy and permissions.")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));
            return;
        }

        await targetMember.TimeoutAsync(expireTime, $"Timed out by {ctx.User.Username}: {reason}");

        var expireTimeTimestamp = expireTime.ToUnixTimeSeconds();

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Successfully timed out **{user.Mention}** until <t:{expireTimeTimestamp}:f> (<t:{expireTimeTimestamp}:R>)!")
            .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(true)));

        await ctx.Channel.SendMessageAsync($"{user.Mention} has been timed out, expiring <t:{expireTimeTimestamp}:R>: **{reason}**");
    }

    [Command("clear")]
    [Description("Clear a timeout before it's set to expire.")]
    public static async Task TimeoutClearCommandAsync(SlashCommandContext ctx,
        [Parameter("member"), Description("The member whose timeout to clear.")]
        DiscordUser user)
    {
        await ctx.DeferResponseAsync(ephemeral: ctx.ShouldUseEphemeralResponse(false));

        DiscordMember targetMember;

        try
        {
            targetMember = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch (NotFoundException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"It doesn't look like **{user.Username}** is in the server, so I can't remove their timeout!"));
            return;
        }

        if (!ctx.Member.CanModerate(targetMember))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"You don't have permission to clear the timeout for **{user.GetFullUsername()}**!")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
            return;
        }

        if (targetMember.CommunicationDisabledUntil is null)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"**{user.GetFullUsername()}** isn't timed out!")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
            return;
        }

        try
        {
            await targetMember.TimeoutAsync(null, $"Timeout cleared manually by {ctx.User.Username}");
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"I don't have permission to clear the timeout for **{user.GetFullUsername()}**! Please check the role hierarchy and permissions.")
                .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
            return;
        }

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Successfully cleared the timeout for {user.Mention}!")
            .AsEphemeral(ephemeral: ctx.ShouldUseEphemeralResponse(false)));
    }
}

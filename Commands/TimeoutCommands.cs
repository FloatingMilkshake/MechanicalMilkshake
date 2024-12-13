namespace MechanicalMilkshake.Commands;

[Command("timeout")]
[Description("Set or clear a timeout for a user.")]
[RequirePermissions(DiscordPermission.ModerateMembers)]
[RequireGuild]
public class TimeoutCmds
{
    [Command("set")]
    [Description("Time out a member.")]
    public static async Task SetTimeout(SlashCommandContext ctx,
        [Parameter("member"), Description("The member to time out.")]
        DiscordUser user,
        [Parameter("duration"), Description("How long the timeout should last. Maximum value is 28 days due to Discord limitations.")]
        string duration,
        [Parameter("reason"), Description("The reason for the timeout.")]
        string reason = "No reason provided.")
    {
        await ctx.DeferResponseAsync(true);

        DiscordMember member;

        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Hmm. It doesn't look like that user is in the server, so I can't time them out.")
                .AsEphemeral());
            return;
        }

        TimeSpan parsedDuration;
        try
        {
            parsedDuration = HumanDateParser.HumanDateParser.Parse(duration)
                .Subtract(ctx.Interaction.CreationTimestamp.DateTime);
        }
        catch
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"I couldn't parse \"{duration}\" as a length of time! Please try again.")
                .AsEphemeral());
            return;
        }

        var expireTime = ctx.Interaction.CreationTimestamp.DateTime + parsedDuration;

        try
        {
            await member.TimeoutAsync(expireTime, reason);
        }
        catch (BadRequestException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    "It looks like you tried to set the timeout duration to more than 28 days in the future! Due to Discord limitations, timeouts can only be up to 28 days.")
                .AsEphemeral());
            return;
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to time out **{UserInfoHelpers.GetFullUsername(user)}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }
        catch (Exception e)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Hmm, something went wrong while trying to time out that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                .AsEphemeral());
            return;
        }

        var dateToConvert = Convert.ToDateTime(expireTime);
        var unixTime = ((DateTimeOffset)dateToConvert).ToUnixTimeSeconds();

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent($"Successfully timed out {user.Mention} until `{expireTime}` (<t:{unixTime}:R>)!")
            .AsEphemeral());
        await ctx.Channel.SendMessageAsync(
            $"{user.Mention} has been timed out, expiring <t:{unixTime}:R>: **{reason}**");
    }

    [Command("clear")]
    [Description("Clear a timeout before it's set to expire.")]
    public static async Task ClearTimeout(SlashCommandContext ctx,
        [Parameter("member"), Description("The member whose timeout to clear.")]
        DiscordUser user)
    {
        await ctx.DeferResponseAsync();

        DiscordMember member;

        try
        {
            member = await ctx.Guild.GetMemberAsync(user.Id);
        }
        catch
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "Hmm. It doesn't look like that user is in the server, so I can't remove their timeout."));
            return;
        }

        try
        {
            await member.TimeoutAsync(null, "Timeout cleared manually.");
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                $"Something went wrong. You or I may not be allowed to clear the timeout for **{UserInfoHelpers.GetFullUsername(user)}**! Please check the role hierarchy and permissions."));
            return;
        }
        catch (Exception e)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Hmm, something went wrong while trying to clear the timeout for that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                .AsEphemeral());
            return;
        }

        await ctx.FollowupAsync(
            new DiscordFollowupMessageBuilder().WithContent(
                $"Successfully cleared the timeout for {user.Mention}!"));
    }
}
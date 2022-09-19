namespace MechanicalMilkshake.Commands;

[SlashRequireGuild]
public class BanCommands : ApplicationCommandModule
{
    [SlashCommand("ban", "Ban a user. They will not be able to rejoin unless unbanned.", false)]
    [SlashCommandPermissions(Permissions.BanMembers)]
    public async Task BanCommand(InteractionContext ctx, [Option("user", "The user to ban.")] DiscordUser userToBan,
        [Option("reason", "The reason for the ban.")]
        string reason = "No reason provided.")
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral());

        try
        {
            await ctx.Guild.BanMemberAsync(userToBan.Id, 0, reason);
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to ban **{userToBan.Username}#{userToBan.Discriminator}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }
        catch (Exception e)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Hmm, something went wrong while trying to ban that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                .AsEphemeral());
            return;
        }

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User banned successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"{userToBan.Mention} has been banned: **{reason}**");
    }

    [SlashCommand("unban", "Unban a user.", false)]
    [SlashCommandPermissions(Permissions.BanMembers)]
    public async Task UnbanCommand(InteractionContext ctx,
        [Option("user", "The user to unban.")] DiscordUser userToUnban)
    {
        await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
            new DiscordInteractionResponseBuilder().AsEphemeral());

        try
        {
            await ctx.Guild.UnbanMemberAsync(userToUnban);
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent(
                    $"Something went wrong. You or I may not be allowed to unban **{userToUnban.Username}#{userToUnban.Discriminator}**! Please check the role hierarchy and permissions.")
                .AsEphemeral());
            return;
        }
        catch (Exception e)
        {
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Hmm, something went wrong while trying to unban that user!\n\nThis was Discord's response:\n> {e.Message}\n\nIf you'd like to contact the bot owner about this, include this debug info:\n```{e}\n```")
                .AsEphemeral());
            return;
        }

        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
            .WithContent("User unbanned successfully.").AsEphemeral());
        await ctx.Channel.SendMessageAsync($"Successfully unbanned **{userToUnban.Username}#{userToUnban.Discriminator}**!");
    }
}
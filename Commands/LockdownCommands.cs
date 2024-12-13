namespace MechanicalMilkshake.Commands;

[Command("lockdown")]
[Description("Lock or unlock a channel.")]
[RequireGuild]
[RequirePermissions(DiscordPermission.ManageChannels, DiscordPermission.ModerateMembers)]
public class Lockdown
{
    [Command("lock")]
    [Description("Lock a channel to prevent members from sending messages.")]
    public static async Task Lock(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(true);

        var existingOverwrites = ctx.Channel.PermissionOverwrites.ToArray();
        var invokerExistingAllowedOverwrites = existingOverwrites.FirstOrDefault(o => o.Id == ctx.Member.Id);
        var botAllowedOverwrites = existingOverwrites.FirstOrDefault(o => o.Id == Program.Discord.CurrentUser.Id);

        try
        {
            // Add initial overwrites for failsafes (bot & user who called /lockdown), deny everyone else Send Messages
            if (invokerExistingAllowedOverwrites is null)
            {
                await ctx.Channel.AddOverwriteAsync(ctx.Member, DiscordPermission.SendMessages, DiscordPermissions.None,
                    "Failsafe for lockdown");
            }
            else
            {
                await ctx.Channel.AddOverwriteAsync(ctx.Member,
                    invokerExistingAllowedOverwrites.Allowed | DiscordPermission.SendMessages,
                    invokerExistingAllowedOverwrites.Denied, "Failsafe for lockdown");
            }

            if (botAllowedOverwrites is null)
            {
                await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id),
                    DiscordPermission.SendMessages, DiscordPermissions.None, "Failsafe for lockdown");
            }
            else
            {
                await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id),
                    botAllowedOverwrites.Allowed | DiscordPermission.SendMessages, botAllowedOverwrites.Denied,
                    "Failsafe for lockdown");
            }

            await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, DiscordPermissions.None, DiscordPermission.SendMessages,
                "Lockdown");

            // Restore overwrites that may have been present before the lockdown

            foreach (var overwrite in existingOverwrites)
                if (overwrite.Type == DiscordOverwriteType.Role)
                {
                    if (await overwrite.GetRoleAsync() == ctx.Guild.EveryoneRole)
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            DiscordPermission.SendMessages | overwrite.Denied,
                            "Restoring previous overrides for lockdown");
                    else
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            overwrite.Denied, "Restoring previous overrides for lockdown");
                }
                else if (overwrite.Id != ctx.Member.Id && overwrite.Id != Program.Discord.CurrentUser.Id)
                {
                    await ctx.Channel.AddOverwriteAsync(await overwrite.GetMemberAsync(), overwrite.Allowed,
                        overwrite.Denied, "Restoring previous overrides for lockdown");
                }
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "I don't have permission to lock this channel! Make sure I have the \"Manage Roles\" server permission, or the \"Manage Permissions\" permission on each channel you want me to be able to lock or unlock."));
            return;
        }

        await ctx.Channel.SendMessageAsync("This channel has been locked.");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("Channel locked successfully.")
            .AsEphemeral());
    }

    [Command("unlock")]
    [Description("Unlock a locked channel to allow members to send messages again.")]
    public static async Task Unlock(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(true);

        try
        {
            // Checking <2 because there will always be 1 overwrite for @everyone permissions
            if (ctx.Channel.PermissionOverwrites.ToArray().Length < 2
                || ctx.Channel.PermissionOverwrites.Any(o => o.Id == ctx.Guild.EveryoneRole.Id &&
                    !o.Denied.HasPermission(DiscordPermission.SendMessages)))
            {
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("This channel is not locked!").AsEphemeral());
                return;
            }
            
            foreach (var permission in ctx.Channel.PermissionOverwrites.ToArray())
                if (permission.Type == DiscordOverwriteType.Role)
                {
                    // Don't touch roles that aren't @everyone
                    if (await permission.GetRoleAsync() != ctx.Guild.EveryoneRole) continue;

                    // Construct new @everyone overwrite; keep it the same but remove denial for Send Messages
                    DiscordOverwriteBuilder newOverwrite = new(ctx.Guild.EveryoneRole)
                    {
                        Allowed = permission.Allowed,
                        Denied = (DiscordPermissions)(permission.Denied - DiscordPermission.SendMessages)
                    };

                    await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, newOverwrite.Allowed,
                        newOverwrite.Denied, "Lockdown cleared");
                }
                else
                {
                    // Filter to user overrides that have Send Messages allowed (failsafes) so we can remove
                    if (!permission.Allowed.HasPermission(DiscordPermission.SendMessages)) continue;

                    // Remove failsafe Send Message overrides
                    DiscordOverwriteBuilder newOverwrite = new(await permission.GetMemberAsync())
                    {
                        Allowed = (DiscordPermissions)(permission.Allowed - DiscordPermission.SendMessages),
                        Denied = permission.Denied
                    };
                    
                    // Check new permission set; if it's totally empty, just drop the override
                    if (newOverwrite.Allowed == DiscordPermissions.None && newOverwrite.Denied == DiscordPermissions.None)
                        await ctx.Channel.DeleteOverwriteAsync(await permission.GetMemberAsync(), "Lockdown cleared");
                    else
                        await ctx.Channel.AddOverwriteAsync(await permission.GetMemberAsync(), newOverwrite.Allowed,
                            newOverwrite.Denied, "Lockdown cleared");
                }
        }
        catch (UnauthorizedException)
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(
                "I don't have permission to unlock this channel! Make sure I have the \"Manage Roles\" server permission, or the \"Manage Permissions\" permission on each channel you want me to be able to lock or unlock."));
            return;
        }

        await ctx.Channel.SendMessageAsync("This channel has been unlocked.");
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Channel unlocked successfully.").AsEphemeral());
    }
}
namespace MechanicalMilkshake.Commands;

[SlashRequireGuild]
public class LockdownCommands : ApplicationCommandModule
{
    [SlashCommandGroup("lockdown", "Lock or unlock a channel.", false)]
    [SlashCommandPermissions(Permissions.ModerateMembers)]
    public class Lockdown
    {
        [SlashCommand("lock", "Lock a channel to prevent members from sending messages.")]
        public static async Task Lock(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            var existingOverwrites = ctx.Channel.PermissionOverwrites.ToArray();
            var invokerExistingAllowedOverwrites = existingOverwrites.FirstOrDefault(o => o.Id == ctx.Member.Id);
            var botAllowedOverwrites = existingOverwrites.FirstOrDefault(o => o.Id == Program.Discord.CurrentUser.Id);

            try
            {
                // Add initial overwrites for failsafes (bot & user who called /lockdown), deny everyone else Send Messages
                if (invokerExistingAllowedOverwrites is null)
                {
                    await ctx.Channel.AddOverwriteAsync(ctx.Member, Permissions.SendMessages, Permissions.None,
                        "Failsafe for lockdown");
                }
                else
                {
                    await ctx.Channel.AddOverwriteAsync(ctx.Member,
                        invokerExistingAllowedOverwrites.Allowed | Permissions.SendMessages,
                        invokerExistingAllowedOverwrites.Denied, "Failsafe for lockdown");
                }

                if (botAllowedOverwrites is null)
                {
                    await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id),
                        Permissions.SendMessages, Permissions.None, "Failsafe for lockdown");
                }
                else
                {
                    await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id),
                        botAllowedOverwrites.Allowed | Permissions.SendMessages, botAllowedOverwrites.Denied,
                        "Failsafe for lockdown");
                }

                await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages,
                    "Lockdown");

                // Restore overwrites that may have been present before the lockdown

                foreach (var overwrite in existingOverwrites)
                    if (overwrite.Type == OverwriteType.Role)
                    {
                        if (await overwrite.GetRoleAsync() == ctx.Guild.EveryoneRole)
                            await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                                Permissions.SendMessages | overwrite.Denied,
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
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I don't have permission to lock this channel! Make sure I have the \"Manage Roles\" server permission, or the \"Manage Permissions\" permission on each channel you want me to be able to lock or unlock."));
                return;
            }

            await ctx.Channel.SendMessageAsync("This channel has been locked.");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Channel locked successfully.")
                .AsEphemeral());
        }

        [SlashCommand("unlock", "Unlock a locked channel to allow members to send messages again.")]
        public static async Task Unlock(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            try
            {
                // Checking <2 because there will always be 1 overwrite for @everyone permissions
                if (ctx.Channel.PermissionOverwrites.ToArray().Length < 2
                    || ctx.Channel.PermissionOverwrites.Any(o => o.Id == ctx.Guild.EveryoneRole.Id &&
                        !o.Denied.HasPermission(Permissions.SendMessages)))
                {
                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                        .WithContent("This channel is not locked!").AsEphemeral());
                    return;
                }
                
                foreach (var permission in ctx.Channel.PermissionOverwrites.ToArray())
                    if (permission.Type == OverwriteType.Role)
                    {
                        // Don't touch roles that aren't @everyone
                        if (await permission.GetRoleAsync() != ctx.Guild.EveryoneRole) continue;

                        // Construct new @everyone overwrite; keep it the same but remove denial for Send Messages
                        DiscordOverwriteBuilder newOverwrite = new(ctx.Guild.EveryoneRole)
                        {
                            Allowed = permission.Allowed,
                            Denied = (Permissions)(permission.Denied - Permissions.SendMessages)
                        };

                        await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, newOverwrite.Allowed,
                            newOverwrite.Denied, "Lockdown cleared");
                    }
                    else
                    {
                        // Filter to user overrides that have Send Messages allowed (failsafes) so we can remove
                        if (!permission.Allowed.HasPermission(Permissions.SendMessages)) continue;

                        // Remove failsafe Send Message overrides
                        DiscordOverwriteBuilder newOverwrite = new(await permission.GetMemberAsync())
                        {
                            Allowed = (Permissions)(permission.Allowed - Permissions.SendMessages),
                            Denied = permission.Denied
                        };
                        
                        // Check new permission set; if it's totally empty, just drop the override
                        if (newOverwrite.Allowed == Permissions.None && newOverwrite.Denied == Permissions.None)
                            await ctx.Channel.DeleteOverwriteAsync(await permission.GetMemberAsync(), "Lockdown cleared");
                        else
                            await ctx.Channel.AddOverwriteAsync(await permission.GetMemberAsync(), newOverwrite.Allowed,
                                newOverwrite.Denied, "Lockdown cleared");
                    }
            }
            catch (UnauthorizedException)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "I don't have permission to unlock this channel! Make sure I have the \"Manage Roles\" server permission, or the \"Manage Permissions\" permission on each channel you want me to be able to lock or unlock."));
                return;
            }

            await ctx.Channel.SendMessageAsync("This channel has been unlocked.");
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Channel unlocked successfully.").AsEphemeral());
        }
    }
}
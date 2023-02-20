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
            
            // Add initial overwrites for failsafes (bot & user who called /lockdown), deny everyone else Send Messages

            //var invokerExistingAllowedOverwrites =


            await ctx.Channel.AddOverwriteAsync(ctx.Member,
                existingOverwrites.FirstOrDefault(o => o.Id == ctx.Member.Id)!.Allowed | Permissions.SendMessages,
                Permissions.None,
                "Failsafe for lockdown");

            await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id),
                existingOverwrites.FirstOrDefault(o => o.Id == Program.Discord.CurrentUser.Id)!.Allowed |
                Permissions.SendMessages, Permissions.None, "Failsafe for lockdown");
            
            await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages,
                "Lockdown");
            
            // Restore overwrites that may have been present before the lockdown

            foreach (var overwrite in existingOverwrites)
                if (overwrite.Type == OverwriteType.Role)
                {
                    if (await overwrite.GetRoleAsync() == ctx.Guild.EveryoneRole)
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            Permissions.SendMessages | overwrite.Denied, "Restoring previous overrides for lockdown");
                    else
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            overwrite.Denied, "Restoring previous overrides for lockdown");
                }
                else if (overwrite.Id != ctx.Member.Id && overwrite.Id != Program.Discord.CurrentUser.Id)
                {
                    await ctx.Channel.AddOverwriteAsync(await overwrite.GetMemberAsync(), overwrite.Allowed,
                        overwrite.Denied, "Restoring previous overrides for lockdown");
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

            foreach (var permission in ctx.Channel.PermissionOverwrites.ToArray())
                if (permission.Type == OverwriteType.Role)
                {
                    // Don't touch roles that aren't @everyone or don't have a deny overwrite for Send Messages
                    if (await permission.GetRoleAsync() != ctx.Guild.EveryoneRole ||
                        !permission.Denied.HasPermission(Permissions.SendMessages)) continue;
                    
                    // Construct new @everyone overwrite; keep it the same but deny Send Messages
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
                    if (!permission.Allowed.HasPermission(Permissions.SendMessages)) continue;
                    
                    // Remove failsafe Send Message overrides
                    DiscordOverwriteBuilder newOverwrite = new(await permission.GetMemberAsync())
                    {
                        Allowed = (Permissions)(permission.Allowed - Permissions.SendMessages),
                        Denied = permission.Denied
                    };

                    await ctx.Channel.AddOverwriteAsync(await permission.GetMemberAsync(), newOverwrite.Allowed,
                        newOverwrite.Denied, "Lockdown cleared");
                }

            await ctx.Channel.SendMessageAsync("This channel has been unlocked.");
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Channel unlocked successfully.").AsEphemeral());
        }
    }
}
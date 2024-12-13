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

        // Get the permissions that are already on the channel, so that we can make sure they are kept when we adjust overwrites for lockdown
        DiscordOverwrite[] existingOverwrites = ctx.Channel.PermissionOverwrites.ToArray();
        
        // Get the bot's permission set from before the lockdown
        var botOverwritesBeforeLockdown = existingOverwrites.Where(x => x.Id == Program.Discord.CurrentUser.Id).FirstOrDefault();

        // Get the bot's allowed permission set
        var botAllowedPermissionsBeforeLockdown = DiscordPermissions.None;
        if (botOverwritesBeforeLockdown is not null)
            botAllowedPermissionsBeforeLockdown = botOverwritesBeforeLockdown.Allowed;
        
        // Get the bot's denied permission set
        var botDeniedPermissionsBeforeLockdown = DiscordPermissions.None;
        if (botOverwritesBeforeLockdown is not null)
            botDeniedPermissionsBeforeLockdown = botOverwritesBeforeLockdown.Denied;
        
        // Get the invoker's permission set from before the lockdown
        var invokerOverwritesBeforeLockdown = existingOverwrites.Where(x => x.Id == ctx.User.Id).FirstOrDefault();
        
        // Get the invoker's allowed permission set
        var invokerAllowedPermissionsBeforeLockdown = DiscordPermissions.None;
        if (invokerOverwritesBeforeLockdown is not null)
            invokerAllowedPermissionsBeforeLockdown = invokerOverwritesBeforeLockdown.Allowed;
        
        // Get the invoker's denied permission set
        var invokerDeniedPermissionsBeforeLockdown = DiscordPermissions.None;
        if (invokerOverwritesBeforeLockdown is not null)
            invokerDeniedPermissionsBeforeLockdown = invokerOverwritesBeforeLockdown.Denied;
        
        // Construct failsafe permission sets
        // Grant Send Messages to the bot and to the invoker in addition to any permissions they might already have,
        var botAllowedPermissions = botAllowedPermissionsBeforeLockdown.Add(DiscordPermission.SendMessages);
        var invokerAllowedPermissions = invokerAllowedPermissionsBeforeLockdown.Add(DiscordPermission.SendMessages);
        
        // Apply failsafes for lockdown
        await ctx.Channel.AddOverwriteAsync(ctx.Channel.Guild.CurrentMember, botAllowedPermissions, botDeniedPermissionsBeforeLockdown, "Failsafe 1 for Lockdown");
        await ctx.Channel.AddOverwriteAsync(ctx.Member, invokerAllowedPermissions, invokerDeniedPermissionsBeforeLockdown, "Failsafe 2 for Lockdown");
        
        // Get the @everyone role's permission set from before the lockdown
        var everyoneOverwritesBeforeLockdown = existingOverwrites.Where(x => x.Id == ctx.Channel.Guild.EveryoneRole.Id).FirstOrDefault();
        
        // Get the @everyone role's allowed permission set
        var everyoneAllowedPermissionsBeforeLockdown = DiscordPermissions.None;
        if (everyoneOverwritesBeforeLockdown is not null)
            everyoneAllowedPermissionsBeforeLockdown = everyoneOverwritesBeforeLockdown.Allowed;
        
        // Get the @everyone role's denied permission set
        var everyoneDeniedPermissionsBeforeLockdown = DiscordPermissions.None;
        if (everyoneOverwritesBeforeLockdown is not null)
            everyoneDeniedPermissionsBeforeLockdown = everyoneOverwritesBeforeLockdown.Denied;
        
        // Construct new @everyone permission set
        var everyoneDeniedPermissions = everyoneDeniedPermissionsBeforeLockdown.Add(DiscordPermission.SendMessages);
        
        // Lock the channel
        await ctx.Channel.AddOverwriteAsync(ctx.Channel.Guild.EveryoneRole, everyoneAllowedPermissionsBeforeLockdown, everyoneDeniedPermissions, $"Lockdown by {ctx.User.Username}");
        
        await ctx.Channel.SendMessageAsync("This channel has been locked.");

        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("Channel locked successfully.")
            .AsEphemeral());
    }

    [Command("unlock")]
    [Description("Unlock a locked channel to allow members to send messages again.")]
    public static async Task Unlock(SlashCommandContext ctx)
    {
        await ctx.DeferResponseAsync(true);

        // Checking <2 because there will always be 1 overwrite for @everyone permissions
        if (ctx.Channel.PermissionOverwrites.ToArray().Length < 2
            || ctx.Channel.PermissionOverwrites.Any(o => o.Id == ctx.Guild.EveryoneRole.Id &&
                !o.Denied.HasPermission(DiscordPermission.SendMessages)))
        {
            await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                .WithContent("This channel is not locked!").AsEphemeral());
            return;
        }
        
        // Get the permissions that are already on the channel, so that we can make sure they are kept when we adjust overwrites for the unlock
        var permissions = ctx.Channel.PermissionOverwrites.ToArray();
        
        // Get bot's permission set from before the unlock
        var botOverwritesBeforeUnlock = permissions.Where(x => x.Id == Program.Discord.CurrentUser.Id).FirstOrDefault();
        
        // Get bot's allowed permission set
        var botAllowedPermissionsBeforeUnlock = DiscordPermissions.None;
        if (botOverwritesBeforeUnlock is not null)
            botAllowedPermissionsBeforeUnlock = botOverwritesBeforeUnlock.Allowed;
        
        // Get bot's denied permission set
        var botDeniedPermissionsBeforeUnlock = DiscordPermissions.None;
        if (botOverwritesBeforeUnlock is not null)
            botDeniedPermissionsBeforeUnlock = botOverwritesBeforeUnlock.Denied;
        
        // Get the invoker role's permission set from before the unlock
        var invokerOverwritesBeforeUnlock = permissions.Where(x => x.Id == ctx.User.Id).FirstOrDefault();
        
        // Get the invoker role's allowed permission set
        var invokerAllowedPermissionsBeforeUnlock = DiscordPermissions.None;
        if (invokerOverwritesBeforeUnlock is not null)
            invokerAllowedPermissionsBeforeUnlock = invokerOverwritesBeforeUnlock.Allowed;
        
        // Get the invoker role's denied permission set
        var invokerDeniedPermissionsBeforeUnlock = DiscordPermissions.None;
        if (invokerOverwritesBeforeUnlock is not null)
            invokerDeniedPermissionsBeforeUnlock = invokerOverwritesBeforeUnlock.Denied;
        
        // Construct new permission sets for bot and invoker
        // Resets Send Messages and Send Messages in Threads for bot and invoker, while preserving other permissions
        var botAllowedPermissions = botAllowedPermissionsBeforeUnlock.Remove(DiscordPermission.SendMessages).Remove(DiscordPermission.SendThreadMessages);
        var invokerAllowedPermissions = invokerAllowedPermissionsBeforeUnlock.Remove(DiscordPermission.SendMessages).Remove(DiscordPermission.SendThreadMessages);
        
        // Get the @everyone role's permission set from before the unlock
        var everyoneOverwritesBeforeUnlock = permissions.Where(x => x.Id == ctx.Channel.Guild.EveryoneRole.Id).FirstOrDefault();
        
        // Get the @everyone role's allowed permission set
        var everyoneAllowedPermissionsBeforeUnlock = DiscordPermissions.None;
        if (everyoneOverwritesBeforeUnlock is not null)
            everyoneAllowedPermissionsBeforeUnlock = everyoneOverwritesBeforeUnlock.Allowed;
        
        // Get the @everyone role's denied permission set
        var everyoneDeniedPermissionsBeforeUnlock = DiscordPermissions.None;
        if (everyoneOverwritesBeforeUnlock is not null)
            everyoneDeniedPermissionsBeforeUnlock = everyoneOverwritesBeforeUnlock.Denied;
        
        // Construct new permission set for @everyone
        // Resets Send Messages while preserving other permissions
        var everyoneDeniedPermissions = everyoneDeniedPermissionsBeforeUnlock.Remove(DiscordPermission.SendMessages);
        
        // Unlock the channel
        await ctx.Channel.AddOverwriteAsync(ctx.Channel.Guild.EveryoneRole, everyoneAllowedPermissionsBeforeUnlock, everyoneDeniedPermissions, $"Unlock by {ctx.User.Username}");
        
        // Remove failsafes
        // For any failsafes where the after-unlock permission set is completely empty, delete the override entirely
        
        if (botAllowedPermissions == DiscordPermissions.None && botDeniedPermissionsBeforeUnlock == DiscordPermissions.None)
            await ctx.Channel.DeleteOverwriteAsync(ctx.Channel.Guild.CurrentMember, "Resetting Lockdown failsafe 1 for unlock");
        else
            await ctx.Channel.AddOverwriteAsync(ctx.Channel.Guild.CurrentMember, botAllowedPermissions, botDeniedPermissionsBeforeUnlock, "Resetting Lockdown failsafe 1 for unlock");
        
        if (invokerAllowedPermissions == DiscordPermissions.None && invokerDeniedPermissionsBeforeUnlock == DiscordPermissions.None)
            await ctx.Channel.DeleteOverwriteAsync(ctx.Member, "Resetting Lockdown failsafe 2 for unlock");
        else
            await ctx.Channel.AddOverwriteAsync(ctx.Member, invokerAllowedPermissions, invokerDeniedPermissionsBeforeUnlock, "Resetting Lockdown failsafe 2 for unlock");
        
        await ctx.Channel.SendMessageAsync("This channel has been unlocked.");
        await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
            .WithContent("Channel unlocked successfully.").AsEphemeral());
    }
}
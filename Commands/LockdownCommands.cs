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

            await ctx.Channel.AddOverwriteAsync(ctx.Member, Permissions.SendMessages);
            await ctx.Channel.AddOverwriteAsync(await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id),
                Permissions.SendMessages);
            await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, Permissions.None, Permissions.SendMessages);

            foreach (var overwrite in existingOverwrites)
                if (overwrite.Type == OverwriteType.Role)
                {
                    if (await overwrite.GetRoleAsync() == ctx.Guild.EveryoneRole)
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            Permissions.SendMessages | overwrite.Denied);
                    else
                        await ctx.Channel.AddOverwriteAsync(await overwrite.GetRoleAsync(), overwrite.Allowed,
                            overwrite.Denied);
                }
                else
                {
                    await ctx.Channel.AddOverwriteAsync(await overwrite.GetMemberAsync(), overwrite.Allowed,
                        overwrite.Denied);
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
                    if (await permission.GetRoleAsync() != ctx.Guild.EveryoneRole ||
                        !permission.Denied.HasPermission(Permissions.SendMessages)) continue;
                    DiscordOverwriteBuilder newOverwrite = new(ctx.Guild.EveryoneRole)
                    {
                        Allowed = permission.Allowed,
                        Denied = (Permissions)(permission.Denied - Permissions.SendMessages)
                    };

                    await ctx.Channel.AddOverwriteAsync(ctx.Guild.EveryoneRole, newOverwrite.Allowed,
                        newOverwrite.Denied);
                }
                else
                {
                    if (await permission.GetMemberAsync() ==
                        await ctx.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id) ||
                        await permission.GetMemberAsync() == ctx.Member)
                        await permission.DeleteAsync();
                }

            await ctx.Channel.SendMessageAsync("This channel has been unlocked.");
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent("Channel unlocked successfully.").AsEphemeral());
        }
    }
}
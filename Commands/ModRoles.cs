namespace MechanicalMilkshake.Commands;

public class ModRoles : ApplicationCommandModule
{
    [SlashCommandGroup("modroles",
        "Get or change mod roles for this server; members with these roles can run Moderator commands.",
        false)]
    [SlashCommandPermissions(Permissions.ModerateMembers)]
    public class ModRolesCommands
    {
        [SlashCommand("get",
            "Get the mod roles for this server.")]
        public static async Task ShowModRoles(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await ModRoleHelpers.UserHasModRole(ctx.Member, ctx.Guild))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "You are not authorized to use this command! Please contact a server admin if you think this is a mistake.")
                    .AsEphemeral());
                return;
            }

            ModRoleConfig modRoles;
            try
            {
                string modRolesSerialized = await Program.Db.HashGetAsync("modroles", ctx.Guild.Id);

                if (string.IsNullOrWhiteSpace(modRolesSerialized))
                {
                    await ctx.FollowUpAsync(
                        new DiscordFollowupMessageBuilder().WithContent(
                            $"This server doesn't have mod roles set up! You can set them with {SlashCmdMentionHelpers.GetSlashCmdMention("modroles", "set")}."));
                    return;
                }

                modRoles = JsonConvert.DeserializeObject<ModRoleConfig>(modRolesSerialized);
            }
            catch (Exception ex)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"I couldn't fetch the mod roles for this server!\n```\n{ex.GetType()}: {ex.Message}\n```"));
                return;
            }

            if (modRoles!.AdminRoleId == default && modRoles!.ModRoleId == default &&
                modRoles!.TrialModRoleId == default)
            {
                await ctx.FollowUpAsync(
                    new DiscordFollowupMessageBuilder().WithContent(
                        "This server doesn't have any mod roles set up!"));
                return;
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    $"Admin Role: {(modRoles.AdminRoleId != default ? $"<@&{modRoles.AdminRoleId}>" : "unset")}"
                    + $"\nMod Role: {(modRoles.ModRoleId != default ? $"<@&{modRoles.ModRoleId}>" : "unset")}"
                    + $"\nTrial Mod Role: {(modRoles.TrialModRoleId != default ? $"<@&{modRoles.TrialModRoleId}>" : "unset")}")
                .AddMentions(Mentions.None));
        }

        [SlashCommand("set", "Set mod roles for this server. Members with these roles can run Moderator commands.")]
        public static async Task SetModRoles(InteractionContext ctx,
            [Option("admin_role", "The role to set as the Admin role. If left empty, the Admin role will be cleared/unset.")] DiscordRole adminRole = default,
            [Option("mod_role", "The role to set as the Mod role. If left empty, the Mod role will be cleared/unset.")] DiscordRole modRole = default,
            [Option("trial_mod_role", "The role to set as the Trial Mod role. If left empty, the trial Mod role will be cleared/unset.")] DiscordRole trialModRole = default)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (!await ModRoleHelpers.UserHasModRole(ctx.Member, ctx.Guild))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "You are not authorized to use this command! Please contact a server admin if you think this is a mistake.")
                    .AsEphemeral());
                return;
            }

            var modRolesSerialized = await Program.Db.HashGetAsync("modroles", ctx.Guild.Id);
            ModRoleConfig modRoles = new();

            if (!string.IsNullOrWhiteSpace(modRolesSerialized))
            {
                modRoles = JsonConvert.DeserializeObject<ModRoleConfig>(modRolesSerialized);

                if (adminRole == default && modRole == default && trialModRole == default)
                {
                    if (modRoles!.AdminRoleId == default && modRoles!.ModRoleId == default && modRoles!.TrialModRoleId == default)
                    {
                        await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                            "Nothing has changed. You didn't have any mod roles set previously, and you haven't chosen any to set!"));
                        return;
                    }

                    await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                        "Did you mean to unset all of your mod roles? If you did, please use `/modroles clear`!"));
                    return;
                }
            }

            await UpdateModRoles(adminRole, modRole, trialModRole, ctx.Guild);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "Okay, I've updated your mod roles as follows:"
                    + $"\n- Admin Role: {(adminRole == null ? modRoles!.AdminRoleId == default ? "unset" : $"<@&{modRoles.AdminRoleId}>" : adminRole.Mention)}"
                    + $"\n- Mod Role: {(modRole == null ? modRoles!.ModRoleId == default ? "unset" : $"<@&{modRoles.ModRoleId}>" : modRole.Mention)}"
                    + $"\n- Trial Mod Role: {(trialModRole == null ? modRoles!.TrialModRoleId == default ? "unset" : $"<@&{modRoles.TrialModRoleId}>" : trialModRole.Mention)}")
                .AddMentions(Mentions.None));
        }

        [SlashCommand("clear", "Clear some or all mod roles for this server.")]
        public static async Task ClearModRoles(InteractionContext ctx)
        {
            if (!await ModRoleHelpers.UserHasModRole(ctx.Member, ctx.Guild))
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                    .WithContent(
                        "You are not authorized to use this command! Please contact a server admin if you think this is a mistake."));
                return;
            }

            var modRolesSerialized = await Program.Db.HashGetAsync("modroles", ctx.Guild.Id);

            if (string.IsNullOrWhiteSpace(modRolesSerialized))
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    "This server doesn't have any mod roles set up, so there are none to clear!"));
                return;
            }

            var modRoles = JsonConvert.DeserializeObject<ModRoleConfig>(modRolesSerialized);

            if (modRoles!.AdminRoleId == default && modRoles!.ModRoleId == default && modRoles!.TrialModRoleId == default)
            {
                await ctx.CreateResponseAsync(
                    new DiscordInteractionResponseBuilder().WithContent(
                        "This server doesn't have any mod roles set up, so there are none to clear!"));
                return;
            }

            var options = new List<DiscordSelectComponentOption>
            {
                new("Admin Role", "admin", "Clear the Admin role."),
                new("Mod Role", "mod", "Clear the Mod role."),
                new("Trial Mod Role", "trialmod", "Clear the Trial Mod role.")
            };
            var dropdown =
                new DiscordSelectComponent("clear-mod-roles-dropdown", null, options, false, 0, options.Count);

            var cancelButton = new DiscordButtonComponent(ButtonStyle.Danger, "cancel-clear-mod-roles-button", "Cancel");

            await ctx.CreateResponseAsync(
                new DiscordInteractionResponseBuilder().WithContent("Choose the roles you would like to clear.")
                    .AddComponents(dropdown).AddComponents(cancelButton));
        }

        [SlashCommand("checkperms", "Check the permissions of a member compared to the configured mod roles.")]
        public static async Task CheckPerms(InteractionContext ctx,
            [Option("member", "The member whose permissions to check.")] DiscordUser user)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);
                
            if (!await ModRoleHelpers.UserHasModRole(ctx.Member, ctx.Guild))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent(
                        "You are not authorized to use this command! Please contact a server admin if you think this is a mistake.")
                    .AsEphemeral());
                return;
            }

            DiscordMember member = default;
            try
            {
                member = await ctx.Guild.GetMemberAsync(user.Id);
            }
            catch
            {
                // User is not in guild.
            }

            var modRolesSerialized = await Program.Db.HashGetAsync("modroles", ctx.Guild.Id);

            if (string.IsNullOrWhiteSpace(modRolesSerialized))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "This server doesn't have any mod roles set up, so I can't check members' permissions against them!"));
                return;
            }

            var modRoles = JsonConvert.DeserializeObject<ModRoleConfig>(modRolesSerialized);

            if (modRoles!.AdminRoleId == default && modRoles!.ModRoleId == default &&
                modRoles!.TrialModRoleId == default)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(
                    "This server doesn't have any mod roles set up, so I can't check members' permissions against them!"));
                return;
            }

            string perms;
            if (member == default)
            {
                perms = $"{user.Mention} isn't in this server!";
            }
            else if (member.Roles.Contains(ctx.Guild.GetRole(modRoles.AdminRoleId)))
                perms = $"{member.Mention} is an Admin.";
            else if (member.Roles.Contains(ctx.Guild.GetRole(modRoles.ModRoleId)))
                perms = $"{member.Mention} is a Mod.";
            else if (member.Roles.Contains(ctx.Guild.GetRole(modRoles.TrialModRoleId)))
                perms = $"{member.Mention} is a Trial Mod.";
            else
                perms = $"{member.Mention} has no special permissions.";

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(perms)
                .AddMentions(Mentions.None));
        }
    }

    private static async Task UpdateModRoles(DiscordRole adminRole, DiscordRole modRole, DiscordRole trialModRole, DiscordGuild guild)
    {
        // We need to store a config for this server. This will be one object per server under a hash for mod roles.
        // Existing configurations should not be overwritten, but instead modified. So they should be fetched first.

        var modRoles = await Program.Db.HashGetAsync("modroles", guild.Id);
        if (modRoles.HasValue)
        {
            // Mod roles have been configured on this server. Get existing role configuration and update accordingly.

            var modRoleConf = JsonConvert.DeserializeObject<ModRoleConfig>(modRoles);

            if (adminRole != default)
                modRoleConf!.AdminRoleId = adminRole.Id;
            if (modRole != default)
                modRoleConf!.ModRoleId = modRole.Id;
            if (trialModRole != default)
                modRoleConf!.TrialModRoleId = trialModRole.Id;

            await Program.Db.HashSetAsync("modroles", guild.Id, JsonConvert.SerializeObject(modRoleConf));
        }
        else
        {
            // Mod roles are not configured for this server. Set up a default/empty config and add to it.

            ModRoleConfig modRoleConf = new();

            if (adminRole != default)
                modRoleConf.AdminRoleId = adminRole.Id;
            if (modRole != default)
                modRoleConf.ModRoleId = modRole.Id;
            if (trialModRole != default)
                modRoleConf.TrialModRoleId = trialModRole.Id;

            await Program.Db.HashSetAsync("modroles", guild.Id, JsonConvert.SerializeObject(modRoleConf));

            // Done. Now this server has a mod role config and the admin role provided has been set.
        }
    }
}
using System.Reflection.Metadata;

namespace MechanicalMilkshake;

public class PerServerFeatures
{
    public class ComplaintSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("complaint", "File a complaint to a specific department.")]
        public async Task Complaint(InteractionContext ctx,
            [Choice("hr", "HR")]
            [Choice("ia", "IA")]
            [Choice("it", "IT")]
            [Choice("corporate", "Corporate")]
            [Option("department", "The department to send the complaint to.")]
            string department,
            [Option("complaint", "Your complaint.")]
            string complaint)
        {
            if (ctx.Guild.Id != 631118217384951808 && ctx.Guild.Id != 984903591816990730 &&
                ctx.Guild.Id != Program.configjson.HomeServerId)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                    .WithContent("This command is not available in this server.").AsEphemeral());
                return;
            }

            if (department != "HR" && department != "IA" && department != "IT" && department != "Corporate")
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                    .WithContent("Please choose from one of the four departments to send your complaint to.")
                    .AsEphemeral());
                return;
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent(
                    $"Your complaint has been recorded. You can see it below. You will be contacted soon about your issue.\n> {complaint}")
                .AsEphemeral());
            var logChannel = await ctx.Client.GetChannelAsync(968515974271741962);
            DiscordMessageBuilder message = new()
            {
                Content =
                    $"{ctx.User.Mention} to {department} (in {ctx.Channel.Mention}/#{ctx.Channel.Name}, {ctx.Guild.Name}):\n> {complaint}"
            };
            await logChannel.SendMessageAsync(message.WithAllowedMentions(Mentions.None));
        }
    }

    public class RoleCommands : ApplicationCommandModule
    {
        [SlashCommand("rolename", "Change the name of your role.")]
        public async Task RoleName(InteractionContext ctx, [Option("name", "The name to change to.")] string name)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                new DiscordInteractionResponseBuilder().AsEphemeral());

            if (ctx.Guild.Id != 984903591816990730)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("This command is not available in this server.").AsEphemeral());
                return;
            }

            List<DiscordRole> roles = new();
            if (ctx.Member.Roles.Any())
            {
                foreach (var role in ctx.Member.Roles.OrderBy(role => role.Position).Reverse()) roles.Add(role);
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You don't have any roles.")
                    .AsEphemeral());
                return;
            }

            if (roles.Count == 1 && (roles.First().Id == 984903591833796659 ||
                                     roles.First().Id == 984903591816990739 ||
                                     roles.First().Id == 984936907874136094))
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You don't have a role that can be renamed!").AsEphemeral());
                return;
            }

            DiscordRole roleToModify = default;
            foreach (var role in roles)
                if (role.Id == 984903591833796659 || role.Id == 984903591816990739 ||
                    role.Id == 984936907874136094)
                {
                }
                else
                {
                    roleToModify = role;
                    break;
                }

            if (roleToModify == default)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You don't have a role that can be renamed!").AsEphemeral());
                return;
            }

            await roleToModify.ModifyAsync(role => role.Name = name);
            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                .WithContent($"Your role has been renamed to **{name}**.").AsEphemeral());
        }
    }

    public class Checks
    {
        public static async Task MessageCreateChecks(MessageCreateEventArgs e)
        {
            if (!e.Channel.IsPrivate && e.Guild.Id == 984903591816990730 && e.Message.Content.StartsWith("ch!"))
            {
                var message = new DiscordMessageBuilder()
                    .WithContent("bots have changed, try `m!` instead.").WithReply(e.Message.Id);
                await e.Channel.SendMessageAsync(message);
            }

            if (e.Channel.Id == 1012735880869466152 && e.Message.Author.Id == 1012735924284702740)
            {
                Regex ipRegex = new(@"[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}");
                string ipAddr = ipRegex.Match(e.Message.Content).ToString();

                Regex attemptCountRegex = new("after ([0-9].*) failed");
                int attemptCount = int.Parse(attemptCountRegex.Match(e.Message.Content).Groups[1].ToString());

                if (attemptCount > 3)
                {
                    var msg = await e.Channel.SendMessageAsync($"<@455432936339144705> `{ipAddr}` attempted to connect {attemptCount} times before being banned. Waiting for approval to ban permanently...");


                    EvalCommands evalCommands = new();
                    await evalCommands.RunCommand($"ssh ubuntu@lxd \"sudo ufw deny from {ipAddr} to any && sudo ufw reload\"");
                    await evalCommands.RunCommand($"ssh ubuntu@lxd \"lxc exec cdnupload -- ufw deny from {ipAddr} to any\"");
                    await evalCommands.RunCommand("ssh ubuntu@lxd \"lxc exec cdnupload -- ufw reload\"");

                    await msg.ModifyAsync($"<@455432936339144705> `{ipAddr}` attempted to connect {attemptCount} times before being banned. It has been permanently banned automatically.");
                }
            }
        }
    }

    public class MessageCommands : BaseCommandModule
    {
        // Per-server commands go here. Use the [TargetServer(serverId)] attribute to restrict a command to a specific guild.

        // Note that this command here can be removed if another command is added; there just needs to be one here to prevent an exception from being thrown when the bot is run.
        [Command("dummycommand")]
        [Hidden]
        public async Task DummyCommand(CommandContext ctx)
        {
            await ctx.RespondAsync(
                "Hi! This command does nothing other than prevent an exception from being thrown when the bot is run. :)");
        }
    }

    public class TargetServerAttribute : CheckBaseAttribute
    {
        public TargetServerAttribute(ulong targetGuild)
        {
            TargetGuild = targetGuild;
        }

        public ulong TargetGuild { get; }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return !ctx.Channel.IsPrivate && ctx.Guild.Id == TargetGuild;
        }
    }
}

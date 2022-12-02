namespace MechanicalMilkshake;

public class PerServerFeatures
{
    public class ComplaintSlashCommands : ApplicationCommandModule
    {
        [SlashCommand("complaint", "File a complaint to a specific department.")]
        public static async Task Complaint(InteractionContext ctx,
            [Choice("hr", "HR")]
            [Choice("ia", "IA")]
            [Choice("it", "IT")]
            [Choice("corporate", "Corporate")]
            [Option("department", "The department to send the complaint to.")]
            string department,
            [Option("complaint", "Your complaint.")] [MaximumLength(4000)]
            string complaint)
        {
            if (ctx.Guild.Id != 631118217384951808 && ctx.Guild.Id != 984903591816990730 &&
                ctx.Guild.Id != Program.HomeServer.Id)
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

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Your complaint has been recorded", Color = Program.BotColor,
                Description =
                    $"You will be contacted soon about your issue. You can see your complaint below.\n> {complaint}"
            }).AsEphemeral());

            var logChannel = await ctx.Client.GetChannelAsync(968515974271741962);
            DiscordEmbedBuilder embed = new()
            {
                Title = "New complaint received!",
                Color = Program.BotColor,
                Description = complaint
            };
            embed.AddField("Sent by", $"{ctx.User.Username}#{ctx.User.Discriminator} (`{ctx.User.Id}`)");
            embed.AddField("Sent from", $"\"{ctx.Guild.Name}\" (`{ctx.Guild.Id}`)");
            embed.AddField("Department", department);
            await logChannel.SendMessageAsync(embed);
        }
    }

    public class RoleCommands : ApplicationCommandModule
    {
        [SlashCommand("rolename", "Change the name of your role.")]
        public static async Task RoleName(InteractionContext ctx,
            [Option("name", "The name to change to.")]
            string name)
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
                roles.AddRange(ctx.Member.Roles.OrderBy(role => role.Position).Reverse());
            }
            else
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("You don't have any roles.")
                    .AsEphemeral());
                return;
            }

            if (roles.Count == 1 && roles.First().Id is 984903591833796659 or 984903591816990739 or 984936907874136094)
            {
                await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                    .WithContent("You don't have a role that can be renamed!").AsEphemeral());
                return;
            }

            DiscordRole roleToModify = default;
            foreach (var role in roles)
                if (role.Id is 984903591833796659 or 984903591816990739 or 984936907874136094)
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

            if (e.Channel.Id == 1012735880869466152 && e.Message.Author.Id == 1012735924284702740 &&
                e.Message.Content.Contains("has banned the IP"))
            {
                Regex ipRegex = new(@"[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}");
                var ipAddr = ipRegex.Match(e.Message.Content).ToString();

                Regex attemptCountRegex = new("after ([0-9].*) failed");
                var attemptCount = int.Parse(attemptCountRegex.Match(e.Message.Content).Groups[1].ToString());

                if (attemptCount > 3)
                {
                    var msg = await e.Channel.SendMessageAsync(
                        $"<@455432936339144705> `{ipAddr}` attempted to connect {attemptCount} times before being banned. Waiting for approval to ban permanently...");


                    await EvalCommands.RunCommand(
                        $"ssh ubuntu@lxd \"sudo ufw deny from {ipAddr} to any && sudo ufw reload\"");
                    await EvalCommands.RunCommand(
                        $"ssh ubuntu@lxd \"lxc exec cdnupload -- ufw deny from {ipAddr} to any\"");
                    await EvalCommands.RunCommand("ssh ubuntu@lxd \"lxc exec cdnupload -- ufw reload\"");

                    await msg.ModifyAsync(
                        $"<@455432936339144705> `{ipAddr}` attempted to connect {attemptCount} times before being banned. It has been permanently banned automatically.");
                }
            }

            if (e.Guild == Program.HomeServer && e.Message.Author.Id == 1031968180974927903 &&
                (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1)).FirstOrDefault().Content
                .Contains("caption"))
            {
                var chan = await Program.Discord.GetChannelAsync(1048242806486999092);
                if (!string.IsNullOrWhiteSpace(e.Message.Content))
                    if (e.Message.Content.Contains("http"))
                        await chan.SendMessageAsync(e.Message.Content);
                else
                    await chan.SendMessageAsync(e.Message.Attachments[0].Url);
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

        private ulong TargetGuild { get; }

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(!ctx.Channel.IsPrivate && ctx.Guild.Id == TargetGuild);
        }
    }
}
namespace MechanicalMilkshake;

public partial class ServerSpecificFeatures
{
    public partial class Checks
    {
        public static async Task MessageCreateChecks(MessageCreateEventArgs e)
        {
            if (!e.Channel.IsPrivate && e.Guild.Id == 1096948488014659728)
            {
                // Get member object to fetch roles
                var member = await e.Guild.GetMemberAsync(e.Author.Id);
                // Check roles, only continue if member does not have either of two specific roles
                if (member.Roles.All(role => role.Id != 1099413365090156655 && role.Id != 1099421482783871107))
                {
                    // Regex to match mentions
                    var mentionRegex = MentionPattern();

                    // Check msg content against regex
                    if (mentionRegex.IsMatch(e.Message.Content))
                    {
                        // Get msg before current
                        var msgBefore = (await e.Channel.GetMessagesBeforeAsync(e.Message.Id, 1))[0];

                        // If msg before also matches pattern, is from same member, has no other content aside from match, and was sent within 30 seconds, time out & warn
                        if (mentionRegex.IsMatch(msgBefore.Content) && msgBefore.Author.Id == e.Author.Id &&
                            msgBefore.CreationTimestamp.AddSeconds(30) > e.Message.CreationTimestamp &&
                            mentionRegex.Matches(msgBefore.Content)[0].Groups[3].Value == string.Empty && mentionRegex.Matches(e.Message.Content)[0].Groups[3].Value == string.Empty &&
                            mentionRegex.Matches(msgBefore.Content)[0].Groups[1].Value == string.Empty && mentionRegex.Matches(e.Message.Content)[0].Groups[1].Value == string.Empty)
                        {
                            await member.ModifyAsync(m => m.CommunicationDisabledUntil = (DateTimeOffset)DateTime.Now.AddMinutes(5));
                            await e.Message.RespondAsync("no mass pings :3");
                        }
                    }
                }
            }

            if (e.Guild == Program.HomeServer && e.Message.Author.Id == 1031968180974927903 &&
                (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1))[0].Content
                .Contains("caption"))
            {
                var chan = await Program.Discord.GetChannelAsync(1048242806486999092);
                if (string.IsNullOrWhiteSpace(e.Message.Content))
                    await chan.SendMessageAsync(e.Message.Attachments[0].Url);
                else if (e.Message.Content.Contains("http"))
                    await chan.SendMessageAsync(e.Message.Content);
            }
        }

        [GeneratedRegex("(.*)?<@!?([0-9]+)>(.*)")]
        private static partial Regex MentionPattern();
    }

    public class MessageCommands : BaseCommandModule
    {
        // Per-server commands go here. Use the [TargetServer(serverId)] attribute to restrict a command to a specific guild.

        [Command("poop")]
        [Description("immaturity is key")]
        [Aliases("shit")]
        [TargetServer(799644062973427743)]
        public async Task Poop(CommandContext ctx)
        {
            if (ctx.Channel.IsPrivate)
            {
                await ctx.RespondAsync("sorry, no can do.");
                return;
            }
            
            try
            {
                var chan = await Program.Discord.GetChannelAsync(892978015309557870);
                var msg = await chan.GetMessageAsync(1085253151155830895);
                
                var phrases = msg.Content.Split("\n");

                await ctx.Channel.SendMessageAsync(phrases[Program.Random.Next(0, phrases.Length)]
                    .Replace("{user}", ctx.Member!.DisplayName));
            }
            catch
            {
                await ctx.RespondAsync("sorry, no can do.");
            }
        }
    }
    
    public class RoleCommands : ApplicationCommandModule
     {
         [SlashCommand("rolename", "Change the name of someone's role.")]
         public static async Task RoleName(InteractionContext ctx,
             [Option("name", "The new name.")] string name,
             [Option("user", "The user whose role name to change.")] DiscordUser user = default)
         {
             await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource,
                 new DiscordInteractionResponseBuilder());

             if (ctx.Guild.Id != 984903591816990730)
             {
                 await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                     .WithContent("This command is not available in this server."));
                 return;
             }

             if (user == default) user = ctx.User;
             DiscordMember member;
             try
             {
                 member = await ctx.Guild.GetMemberAsync(user.Id);
             }
             catch
             {
                 await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that user!"));
                 return;
             }

             List<DiscordRole> roles = new();
             if (member.Roles.Any())
             {
                 roles.AddRange(member.Roles.OrderBy(role => role.Position).Reverse());
             }
             else
             {
                 var response = ctx.User == user ? "You don't have any roles." : "That user doesn't have any roles.";
                 await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
                 return;
             }

             if (roles.Count == 1 && roles.First().Id is 984903591833796659 or 984903591816990739 or 984936907874136094)
             {
                 var response = ctx.User == user
                     ? "You don't have a role that can be renamed!"
                     : "That user doesn't have a role that can be renamed!";
                 await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(response));
                 return;
             }

             var roleToModify = roles.FirstOrDefault(role =>
                 role.Id is not (984903591833796659 or 984903591816990739 or 984936907874136094));

             if (roleToModify == default)
             {
                 var response = ctx.User == user
                     ? "You don't have a role that can be renamed!"
                     : "That user doesn't have a role that can be renamed!";
                 await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder()
                     .WithContent(response));
                 return;
             }

             try
             {
                 await roleToModify.ModifyAsync(role => role.Name = name);
             }
             catch (UnauthorizedException)
             {
                 await ctx.FollowUpAsync(
                     new DiscordFollowupMessageBuilder().WithContent("I don't have permission to rename that role!"));
                 return;
             }
             
             var finalResponse = ctx.User == user
                 ? $"Your role has been renamed to **{name}**."
                 : $"{member.Mention}'s role has been renamed to **{name}**.";
             await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(finalResponse));
         }
     }


    private class TargetServerAttribute(ulong targetGuild) : CheckBaseAttribute
    {

        private ulong TargetGuild { get; } = targetGuild;

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(!ctx.Channel.IsPrivate && ctx.Guild.Id == TargetGuild);
        }
    }
}

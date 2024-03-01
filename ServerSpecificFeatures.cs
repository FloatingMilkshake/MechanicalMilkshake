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

    private class TargetServerAttribute(ulong targetGuild) : CheckBaseAttribute
    {

        private ulong TargetGuild { get; } = targetGuild;

        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(!ctx.Channel.IsPrivate && ctx.Guild.Id == TargetGuild);
        }
    }
}
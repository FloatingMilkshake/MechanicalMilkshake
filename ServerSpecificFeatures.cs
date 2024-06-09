namespace MechanicalMilkshake;

public partial class ServerSpecificFeatures
{
    public partial class Checks
    {
        public static async Task MessageCreateChecks(MessageCreateEventArgs e)
        {
            if (e.Guild.Id == 799644062973427743)
            {
                // &caption -> #captions
                if (e.Message.Author.Id == 1031968180974927903 &&
                    (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1))[0].Content
                    .Contains("caption"))
                {
                    var chan = await Program.Discord.GetChannelAsync(1048242806486999092);
                    if (string.IsNullOrWhiteSpace(e.Message.Content))
                        await chan.SendMessageAsync(e.Message.Attachments[0].Url);
                    else if (e.Message.Content.Contains("http"))
                        await chan.SendMessageAsync(e.Message.Content);
                }
                
                // parse Windows Insiders RSS feed
                if (e.Message.Author.Id == 944784076735414342 && e.Message.Channel.Id == 1227636018375819296)
                {
                    // ignore no-content messages
                    if (e.Message.Content is null) return;
                    
                    // try to match content with Insider URL pattern
                    var insiderUrlPattern = InsiderUrlPattern();
                    
                    // ignore non-matching messages
                    if (!insiderUrlPattern.IsMatch(e.Message.Content)) return;
                    
                    var insiderUrlMatch = insiderUrlPattern.Match(e.Message.Content);
                    var windowsVersion = $"Windows {insiderUrlMatch.Groups[1].Value}";
                    var buildNumber1 = insiderUrlMatch.Groups[2].Value;
                    var buildNumber2 = insiderUrlMatch.Groups[3].Value;
                    var channel1 = insiderUrlMatch.Groups[4].Value;
                    var channel2 = insiderUrlMatch.Groups[5].Value;
                    
                    // format channel names correctly
                    // canary -> Canary Channel
                    // dev -> Dev Channel
                    // beta -> Beta Channel
                    // release-preview -> Release Preview Channel
                    
                    channel1 = channel1 switch
                    {
                        "canary" => "Canary Channel",
                        "dev" => "Dev Channel",
                        "beta" => "Beta Channel",
                        "release-preview" => "Release Preview Channel",
                        _ => string.Empty
                    };
                    
                    channel2 = channel2 switch
                    {
                        "canary" => "Canary Channel",
                        "dev" => "Dev Channel",
                        "beta" => "Beta Channel",
                        "release-preview" => "Release Preview Channel",
                        _ => string.Empty
                    };
                    
                    // assemble /announcebuild command
                    // format is: /announcebuild windows_version:WINDOWS_VERSION build_number:BUILD_NUMBER blog_link:BLOG_LINK insider_role1:FIRST_ROLE insider_role2:SECOND_ROLE
                    // insider_role2 is optional
                    // if two build numbers are present, pick the higher one
                    // if a build number contains a hyphen, replace it with a dot (ex. 22635-3430 -> 22635.3430)

                    var buildNumber = buildNumber2 == string.Empty
                        ? buildNumber1
                        : string.Compare(buildNumber1, buildNumber2, StringComparison.Ordinal) > 0
                            ? buildNumber1
                            : buildNumber2;
                    buildNumber = buildNumber.Replace('-', '.');
                    
                    var blogLink = insiderUrlMatch.ToString();
                    
                    var command = $"/announcebuild windows_version:{windowsVersion} build_number:{buildNumber} blog_link:{blogLink} insider_role1:{channel1}";
                    if (channel2 != string.Empty) command += $" insider_role2:{channel2}";
                    
                    // send command to channel
                    var msg = await e.Message.Channel.SendMessageAsync(command);
                    
                    // suppress embed
                    await Task.Delay(1000);
                    await msg.ModifyEmbedSuppressionAsync(true);
                }
            }
        }

        [GeneratedRegex("(.*)?<@!?([0-9]+)>(.*)")]
        private static partial Regex MentionPattern();
        [GeneratedRegex(@"https.*windows-(\d+).*?build[s]?-(?:(\d+(?:-\d+)?)(?:-and-(\d+-\d+)?)*)(?:.+?(?:(canary|dev|beta|release-preview)(?:-and-(canary|dev|beta|release-preview))*)?-channel[s]?.*)?\/")]
        private static partial Regex InsiderUrlPattern();
    }

    public class Events
    {
        public static async Task GuildMemberUpdated(DiscordClient client, GuildMemberUpdateEventArgs e)
        {
            if (e.Member.Id == 455432936339144705)
            {
                // get new avatar
                var newAvatarUrl = $"https://cdn.discordapp.com/avatars/{e.Member.Id}/{e.AvatarHashAfter}.png?size=4096";
                
                // upload to cdn
                
                MemoryStream memStream = new(await Program.HttpClient.GetByteArrayAsync(newAvatarUrl));
                
                var args = new PutObjectArgs()
                    .WithBucket("cdn")
                    .WithObject("avatar_TEST.png")
                    .WithStreamData(memStream)
                    .WithObjectSize(memStream.Length)
                    .WithContentType("image/png");

                await Program.Minio.PutObjectAsync(args);
            }
        }
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

namespace MechanicalMilkshake;

public partial class ServerSpecificFeatures
{
    public partial class Checks
    {
        public static async Task MessageCreateChecks(MessageCreateEventArgs e)
        {
            if (e.Channel.IsPrivate) return;
            
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
            
#if DEBUG
            if (e.Guild.Id == 799644062973427743) // my testing server
            {
                await PatchTuesdayAnnouncementCheck(e, 455432936339144705, 882446411130601472);
            }
#else
            if (e.Guild.Id == 438781053675634713) // not my testing server
            {
                await PatchTuesdayAnnouncementCheck(e, 696333378990899301, 1251028070488477716);
            }
#endif
            
            // Redis "shutCooldowns" hash:
            // Key user ID
            // Value whether user has attempted to use the command during cooldown
            // Vaolue is used so that on user's first attempt during cooldown, we can respond to indicate the cooldown;
            // but on subsequent attempts we should not respond to avoid ratelimits as that would defeat the purpose of the cooldown

            if (e.Guild.Id == 1203128266559328286 || e.Guild.Id == Program.HomeServer.Id)
            {
                if (e.Message.Content is not null && !e.Message.Author.IsBot && (e.Message.Content.Equals("shut", StringComparison.OrdinalIgnoreCase) || e.Message.Content.Equals("open", StringComparison.OrdinalIgnoreCase)))
                {
                    var userId = e.Message.Author.Id;
                    var userShutCooldownSerialized = await Program.Db.HashGetAsync("shutCooldowns", userId.ToString());
                    KeyValuePair<DateTime, bool> userShutCooldown;
                    if (userShutCooldownSerialized.HasValue)
                    {
                        try
                        {
                            userShutCooldown = JsonConvert.DeserializeObject<KeyValuePair<DateTime, bool>>(userShutCooldownSerialized);
                        }
                        catch (Exception ex)
                        {
                            Program.Discord.Logger.LogWarning("Failed to read shut cooldown from db for user {user}! {exType}: {exMessage}\n{exStackTrace}", userId, ex.GetType(), ex.Message, ex.StackTrace);
                            userShutCooldown = new();
                        }
                        
                        var userCooldownTime = userShutCooldown.Key;
                        if (userCooldownTime > DateTime.Now && !userShutCooldown.Value) // user on cooldown & has not attempted
                        {
                            var cooldownRemainingTime = Math.Round((userCooldownTime - DateTime.Now).TotalSeconds);
                            if (cooldownRemainingTime == 0) cooldownRemainingTime = 1;
                            await e.Message.RespondAsync($"You're going too fast! Try again in {cooldownRemainingTime} second{(cooldownRemainingTime > 1 ? "s" : "")}.");
                            userShutCooldown = new(userShutCooldown.Key, true);
                        }
                        else if (userCooldownTime < DateTime.Now)
                        {
                            userShutCooldown = new KeyValuePair<DateTime, bool>(DateTime.Now.AddSeconds(5), false);
                        
                            if (e.Message.Content.Equals("shut", StringComparison.OrdinalIgnoreCase))
                                await e.Message.RespondAsync("open");
                            else if (e.Message.Content.Equals("open", StringComparison.OrdinalIgnoreCase))
                                await e.Message.RespondAsync("shut");
                        }
                    }
                    else
                    {
                        if (e.Message.Content.Equals("shut", StringComparison.OrdinalIgnoreCase))
                            await e.Message.RespondAsync("open");
                        else if (e.Message.Content.Equals("open", StringComparison.OrdinalIgnoreCase))
                            await e.Message.RespondAsync("shut");
                        
                        userShutCooldown = new(DateTime.Now.AddSeconds(5), false);
                    }
                    
                    await Program.Db.HashSetAsync("shutCooldowns", userId.ToString(), JsonConvert.SerializeObject(userShutCooldown));
                }
            }
        }

        private static async Task PatchTuesdayAnnouncementCheck(MessageCreateEventArgs e, ulong authorId, ulong channelId)
        {
            // Patch Tuesday automatic message generation

            if (e.Message.Author.Id != authorId || e.Channel.Id != channelId)
                return;
            
            // List of roles to ping with message
            var usersToPing = new List<ulong>
            {
                228574821590499329,
                455432936339144705
            };
            
            // Get message before current message; if authors do not match or message is not a Cumulative Updates post, ignore
            var previousMessage = (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1))[0];
            if (previousMessage.Author.Id != e.Message.Author.Id || !previousMessage.Content.Contains("Cumulative Updates"))
                return;
            
            // Get URLs from both messages
            var insiderRedditUrlPattern = new Regex(@"https:\/\/.*reddit.com\/r\/Windows[0-9]{1,}.*cumulative_updates.*");
            var thisUrl = insiderRedditUrlPattern.Match(e.Message.Content).Value;
            var previousUrl = insiderRedditUrlPattern.Match(previousMessage.Content).Value;
            
            // Figure out which URL is Windows 10 and which is Windows 11
            var windows10Url = thisUrl.Contains("Windows10") ? thisUrl : previousUrl;
            var windows11Url = thisUrl.Contains("Windows11") ? thisUrl : previousUrl;
            
            // Assemble message
            var msg = "";

            foreach (var user in usersToPing)
                msg += $"<@{user}> ";
            
            msg += $"```\nIt's @Patch Tuesday! Update discussion threads & changelist links are here: {windows10Url} (Windows 10) and {windows11Url} (Windows 11)\n```";
            
            // Send message
            await e.Message.Channel.SendMessageAsync(msg);
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
                    .WithObject("avatar.png")
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
                // ReSharper disable JoinDeclarationAndInitializer
                DiscordChannel chan;
                DiscordMessage msg;
                // ReSharper restore JoinDeclarationAndInitializer
                #if DEBUG
                chan = await Program.Discord.GetChannelAsync(893654247709741088);
                msg = await chan.GetMessageAsync(1282187612844589168);
                #else
                chan = await Program.Discord.GetChannelAsync(892978015309557870);
                msg = await chan.GetMessageAsync(1085253151155830895);
                #endif
                
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

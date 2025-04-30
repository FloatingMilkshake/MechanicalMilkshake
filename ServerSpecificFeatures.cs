namespace MechanicalMilkshake;

public partial class ServerSpecificFeatures
{
    public partial class EventChecks
    {
        public static bool ShutBotsAllowed;
        
        public static async Task MessageCreateChecks(MessageCreatedEventArgs e)
        {
            // ignore dms
            if (e.Channel.IsPrivate) return;
            
            #region dev/home server
            if (e.Guild.Id == 799644062973427743)
            {
                #region &caption -> #captions
                if (e.Message.Author.Id == 1031968180974927903 &&
                    (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1).ToListAsync())[0].Content
                    .Contains("caption"))
                {
                    var chan = await Program.Discord.GetChannelAsync(1048242806486999092);
                    if (string.IsNullOrWhiteSpace(e.Message.Content))
                        await chan.SendMessageAsync(e.Message.Attachments[0].Url);
                    else if (e.Message.Content.Contains("http"))
                        await chan.SendMessageAsync(e.Message.Content);
                }
                #endregion &caption -> #captions
            }
            #endregion dev/home server
            
            #region Patch Tuesday announcements
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
            #endregion Patch Tuesday announcements
            
            #region shut
            // Redis "shutCooldowns" hash:
            // Key user ID
            // Value whether user has attempted to use the command during cooldown
            // Value is used so that on user's first attempt during cooldown, we can respond to indicate the cooldown;
            // but on subsequent attempts we should not respond to avoid ratelimits as that would defeat the purpose of the cooldown

            if (e.Guild.Id == 1203128266559328286 || e.Guild.Id == Program.HomeServer.Id)
            {
                if (e.Message.Content == "shutok" && e.Message.Author.Id == 455432936339144705)
                {
                    await e.Message.RespondAsync("ok");
                    ShutBotsAllowed = true;
                }
                
                if (e.Message.Content == "shutstop" && e.Message.Author.Id == 455432936339144705)
                {
                    await e.Message.RespondAsync("ok");
                    ShutBotsAllowed = false;
                }
                
                if (e.Message.Content is not null && (e.Message.Content.Equals("shut", StringComparison.OrdinalIgnoreCase) || e.Message.Content.Equals("open", StringComparison.OrdinalIgnoreCase)
                    || e.Message.Content.Equals("**shut**", StringComparison.OrdinalIgnoreCase) || e.Message.Content.Equals("**open**", StringComparison.OrdinalIgnoreCase)))
                {
                    if (e.Message.Author.IsBot)
                        if (!ShutBotsAllowed || e.Message.Author.Id == Program.Discord.CurrentApplication.Id || e.Channel.Id != 1285684652543185047) return; // testing uwubot? use 1285760935310655568 for channel id
                    
                    var userId = e.Message.Author.Id;
                    var userShutCooldownSerialized = await Program.Db.HashGetAsync("shutCooldowns", userId.ToString());
                    KeyValuePair<DateTime, bool> userShutCooldown = new();
                    if (userShutCooldownSerialized.HasValue)
                    {
                        try
                        {
                            userShutCooldown = JsonConvert.DeserializeObject<KeyValuePair<DateTime, bool>>(userShutCooldownSerialized);
                        }
                        catch (Exception ex)
                        {
                            Program.Discord.Logger.LogWarning("Failed to read shut cooldown from db for user {user}! {exType}: {exMessage}\n{exStackTrace}", userId, ex.GetType(), ex.Message, ex.StackTrace);
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
                        
                            if (e.Message.Content.Equals("shut", StringComparison.OrdinalIgnoreCase)
                                    || e.Message.Content.Equals("**shut**", StringComparison.OrdinalIgnoreCase))
                                await e.Message.RespondAsync("open");
                            else if (e.Message.Content.Equals("open", StringComparison.OrdinalIgnoreCase)
                                     || e.Message.Content.Equals("**open**", StringComparison.OrdinalIgnoreCase))
                                await e.Message.RespondAsync("shut");
                        }
                    }
                    else
                    {
                        if (e.Message.Content.Equals("shut", StringComparison.OrdinalIgnoreCase)
                                || e.Message.Content.Equals("**shut**", StringComparison.OrdinalIgnoreCase))
                            await e.Message.RespondAsync("open");
                        else if (e.Message.Content.Equals("open", StringComparison.OrdinalIgnoreCase) 
                                 || e.Message.Content.Equals("**open**", StringComparison.OrdinalIgnoreCase))
                            await e.Message.RespondAsync("shut");
                        
                        if (e.Message.Author.Id != 1264728368847523850) // testing uwubot? use 1285760461047861298
                            userShutCooldown = new(DateTime.Now.AddSeconds(5), false);
                    }
                    
                    if (userShutCooldown.Key == DateTime.MinValue && userShutCooldown.Value == false)
                        await Program.Db.HashSetAsync("shutCooldowns", userId.ToString(), JsonConvert.SerializeObject(userShutCooldown));
                }
            }
            #endregion shut
        }

        private static async Task PatchTuesdayAnnouncementCheck(MessageCreatedEventArgs e, ulong authorId, ulong channelId)
        {
            // Patch Tuesday automatic message generation
            
            var insiderRedditUrlPattern = new Regex(@"https:\/\/.*reddit.com\/r\/Windows[0-9]{1,}.*cumulative_updates.*");

            // Filter to messages by passed author & channel IDs, and that match the pattern
            if (e.Message.Author.Id != authorId || e.Channel.Id != channelId || !insiderRedditUrlPattern.IsMatch(e.Message.Content))
                return;
            
            // List of roles to ping with message
            var usersToPing = new List<ulong>
            {
                228574821590499329,
                455432936339144705
            };
            
            // Get message before current message; if authors do not match or message is not a Cumulative Updates post, ignore
            var previousMessage = (await e.Message.Channel.GetMessagesBeforeAsync(e.Message.Id, 1).ToListAsync())[0];
            if (previousMessage.Author.Id != e.Message.Author.Id || !insiderRedditUrlPattern.IsMatch(previousMessage.Content))
                return;
            
            // Get URLs from both messages
            var thisUrl = insiderRedditUrlPattern.Match(e.Message.Content).Value;
            var previousUrl = insiderRedditUrlPattern.Match(previousMessage.Content).Value;
            
            // Figure out which URL is Windows 10 and which is Windows 11
            var windows10Url = thisUrl.Contains("Windows10") ? thisUrl : previousUrl;
            var windows11Url = thisUrl.Contains("Windows11") ? thisUrl : previousUrl;
            
            // Assemble message
            var msg = "";

            foreach (var user in usersToPing)
                msg += $"<@{user}> ";
            
            msg += $"```\nIt's <@&445773142233710594>! Update discussion threads & changelist links are here: {windows10Url} (Windows 10) and {windows11Url} (Windows 11)\n```";
            
            // Send message
            await e.Message.Channel.SendMessageAsync(msg);
        }

        #region regex
        [GeneratedRegex("(.*)?<@!?([0-9]+)>(.*)")]
        private static partial Regex MentionPattern();
        [GeneratedRegex(@"https.*windows-(\d+).*?build[s]?-(?:(\d+(?:-\d+)?)(?:-and-(\d+-\d+)?)*)(?:.+?(?:(canary|dev|beta|release-preview)(?:-and-(canary|dev|beta|release-preview))*)?-channel[s]?.*)?\/")]
        private static partial Regex InsiderUrlPattern();
        #endregion regex
    }
    
    public class Commands
    {
        public class MessageCommands
        {
            // Per-server commands go here. Use the [TargetServer(serverId)] attribute to restrict a command to a specific guild.

            [Command("poop")]
            [Description("immaturity is key")]
            [TextAlias("shit", "defecate")]
            [CommandChecks.AllowedServers(799644062973427743, 1203128266559328286)]
            [AllowedProcessors(typeof(TextCommandProcessor))]
            public async Task Poop(CommandContext ctx, [RemainingText] string much = "")
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
                    msg = much == "MUCH" ? await chan.GetMessageAsync(1294869494648279071) : await chan.GetMessageAsync(1085253151155830895);
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
    
        public class RoleCommands
        {
            [Command("rolename")]
            [Description("Change the name of someone's role.")]
            [AllowedProcessors(typeof(SlashCommandProcessor))]
            public static async Task RoleName(MechanicalMilkshake.SlashCommandContext ctx,
                [Parameter("name"), Description("The new name.")] string name,
                [Parameter("user"), Description("The user whose role name to change.")] DiscordUser user = default)
            {
                await ctx.DeferResponseAsync();

                if (ctx.Guild.Id != 984903591816990730)
                {
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
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
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent("I couldn't find that user!"));
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
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(response));
                    return;
                }

                if (roles.Count == 1 && roles.First().Id is 984903591833796659 or 984903591816990739 or 984936907874136094)
                {
                    var response = ctx.User == user
                        ? "You don't have a role that can be renamed!"
                        : "That user doesn't have a role that can be renamed!";
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(response));
                    return;
                }

                var roleToModify = roles.FirstOrDefault(role =>
                    role.Id is not (984903591833796659 or 984903591816990739 or 984936907874136094));

                if (roleToModify == default)
                {
                    var response = ctx.User == user
                        ? "You don't have a role that can be renamed!"
                        : "That user doesn't have a role that can be renamed!";
                    await ctx.FollowupAsync(new DiscordFollowupMessageBuilder()
                        .WithContent(response));
                    return;
                }

                try
                {
                    await roleToModify.ModifyAsync(role => role.Name = name);
                }
                catch (UnauthorizedException)
                {
                    await ctx.FollowupAsync(
                        new DiscordFollowupMessageBuilder().WithContent("I don't have permission to rename that role!"));
                    return;
                }
                
                var finalResponse = ctx.User == user
                    ? $"Your role has been renamed to **{name}**."
                    : $"{member.Mention}'s role has been renamed to **{name}**.";
                await ctx.FollowupAsync(new DiscordFollowupMessageBuilder().WithContent(finalResponse));
            }
        }
    }
    
    public class CommandChecks
    {
        public class AllowedServersAttribute(params ulong[] allowedServers) : ContextCheckAttribute
        {
            public ulong[] AllowedServers { get; } = allowedServers;
        }
    
        public class AllowedServersContextCheck : IContextCheck
        {
#nullable enable
            public ValueTask<string?> ExecuteCheckAsync(AllowedServersAttribute attribute, CommandContext ctx) =>
                ValueTask.FromResult(!ctx.Channel.IsPrivate && ctx.Guild is not null && attribute.AllowedServers.Contains(ctx.Guild.Id)
                    ? null
                    : "This command is not available in this server.");
        }
    }
}

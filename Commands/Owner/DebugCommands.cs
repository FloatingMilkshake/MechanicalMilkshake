namespace MechanicalMilkshake.Commands.Owner;

[SlashRequireAuth]
public class DebugCommands : ApplicationCommandModule
{
    [SlashCommandGroup("debug", "[Authorized users only] Commands for checking if the bot is working properly.")]
    public class DebugCmds : ApplicationCommandModule
    {
        [SlashCommand("info", "[Authorized users only] Show debug information about the bot.")]
        public async Task DebugInfo(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = "Debug Info",
                Color = new DiscordColor("#FAA61A")
            };

            var debugInfo = DebugInfoHelper.GetDebugInfo();

            embed.AddField("Framework", debugInfo.Framework, true);
            embed.AddField("Platform", debugInfo.Platform, true);
            embed.AddField("Library", debugInfo.Library, true);
            embed.AddField("Commit Hash", $"`{debugInfo.CommitHash}`", true);
            embed.AddField(debugInfo.CommitTimeDescription, debugInfo.CommitTimestamp, true);
            embed.AddField("Commit Message", debugInfo.CommitMessage, false);

            embed.AddField("Server Count", Program.discord.Guilds.Count.ToString(), true);

            int commandCount;
#if DEBUG
            commandCount = (await Program.discord.GetGuildApplicationCommandsAsync(Program.configjson.HomeServerId)).Count;
#else
        commandCount = (await Program.discord.GetGlobalApplicationCommandsAsync()).Count;
#endif
            embed.AddField("Command Count", commandCount.ToString(), true);
            embed.AddField("Load Time", debugInfo.LoadTime, true);

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("uptime", "[Authorized users only] Check the bot's uptime (from the time it connects to Discord).")]
        public async Task Uptime(InteractionContext ctx)
        {
            var connectUnixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();

            var startTime = Convert.ToDateTime(Program.processStartTime);
            var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                $"Process started at <t:{startUnixTime}:F> (<t:{startUnixTime}:R>).\n\nLast connected to Discord at <t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>)."));
        }

        [SlashCommand("timecheck", "[Authorized users only] Return the current time on the machine the bot is running on.")]
        public async Task TimeCheck(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                $"Seems to me like it's currently `{DateTime.Now}`."
                + $"\n(Short Time: `{DateTime.Now.ToShortTimeString()}`)"));
        }

        [SlashCommand("shutdown", "[Authorized users only] Shut down the bot.")]
        public async Task Shutdown(InteractionContext ctx)
        {
            DiscordButtonComponent shutdownButton = new(ButtonStyle.Danger, "shutdown-button", "Shut Down");
            DiscordButtonComponent cancelButton = new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel");

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                .WithContent("Are you sure you want to shut down the bot? This action cannot be undone.")
                .AddComponents(shutdownButton, cancelButton));
        }

        [SlashCommand("restart", "[Authorized users only] Restart the bot.")]
        public async Task Restart(InteractionContext ctx)
        {
            try
            {
                var dockerCheckFile = File.ReadAllText("/proc/self/cgroup");
                if (string.IsNullOrWhiteSpace(dockerCheckFile))
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                        "The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`."
                        + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot."));
                    return;
                }
            }
            catch
            {
                // /proc/self/cgroup could not be found, which means the bot is not running in Docker.
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    "The bot may not be running under Docker; this means that `/debug restart` will behave like `/debug shutdown`.)"
                    + "\n\nOperation aborted. Use `/debug shutdown` if you wish to shut down the bot."));
                return;
            }

            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("Restarting..."));
            Environment.Exit(1);
        }

        [SlashCommand("owners", "[Authorized users only] Show the bot's owners.")]
        public async Task Owners(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            List<DiscordUser> botOwners = new();
            List<DiscordUser> authorizedUsers = new();

            foreach (var owner in ctx.Client.CurrentApplication.Owners) botOwners.Add(owner);

            foreach (var userId in Program.configjson.AuthorizedUsers)
                authorizedUsers.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));

            var ownerOutput = "Bot owners are:";

            foreach (var owner in botOwners) ownerOutput += $"\n- {owner.Username}#{owner.Discriminator}";

            ownerOutput = ownerOutput.Trim() + "\n\nUsers authorized to use owner-level commands are:";

            foreach (var user in authorizedUsers) ownerOutput += $"\n- {user.Username}#{user.Discriminator}";

            ownerOutput = ownerOutput.Trim();

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent(ownerOutput));
        }

        [SlashCommand("guilds", "[Authorized users only] Show the guilds that the bot is in.")]
        public async Task Guilds(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = "Joined Guilds"
            };

            foreach (var guild in Program.discord.Guilds)
                embed.Description += $"- `{guild.Value.Id}`: {guild.Value.Name}\n";

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }
    }
}
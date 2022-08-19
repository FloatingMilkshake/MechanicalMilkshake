namespace MechanicalMilkshake.Commands.Owner.HomeServerCommands
{
    public class DebugCommands : ApplicationCommandModule
    {
        [SlashCommandGroup("debug", "Commands for checking if the bot is working properly.")]
        public class DebugCmds : ApplicationCommandModule
        {
            [SlashCommand("info", "Show debug information about the bot.")]
            public async Task DebugInfo(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(
                    new DiscordInteractionResponseBuilder().WithContent("Debug Information:\n" +
                                                                        DebugInfoHelper.GetDebugInfo()));
            }

            [SlashCommand("uptime", "Check the bot's uptime (from the time it connects to Discord).")]
            public async Task Uptime(InteractionContext ctx)
            {
                var connectUnixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();

                var startTime = Convert.ToDateTime(Program.processStartTime);
                var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"Process started at <t:{startUnixTime}:F> (<t:{startUnixTime}:R>).\n\nLast connected to Discord at <t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>)."));
            }

            [SlashCommand("timecheck", "Return the current time on the machine the bot is running on.")]
            public async Task TimeCheck(InteractionContext ctx)
            {
                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent(
                    $"Seems to me like it's currently `{DateTime.Now}`."
                    + $"\n(Short Time: `{DateTime.Now.ToShortTimeString()}`)"));
            }

            [SlashCommand("shutdown", "Shut down the bot.")]
            public async Task Shutdown(InteractionContext ctx)
            {
                DiscordButtonComponent shutdownButton = new(ButtonStyle.Danger, "shutdown-button", "Shut Down");
                DiscordButtonComponent cancelButton = new(ButtonStyle.Primary, "shutdown-cancel-button", "Cancel");

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder()
                    .WithContent("Are you sure you want to shut down the bot? This action cannot be undone.")
                    .AddComponents(shutdownButton, cancelButton));
            }

            [SlashCommand("restart", "Restart the bot.")]
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
        }
    }
}

using HumanDateParser;
using MechanicalMilkshake.Checks;

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

            await ctx.FollowUpAsync(
                new DiscordFollowupMessageBuilder().AddEmbed(await DebugInfoHelpers.GenerateDebugInfoEmbed(false)));
        }

        [SlashCommand("uptime",
            "[Authorized users only] Check the bot's uptime (from the time it connects to Discord).")]
        public async Task Uptime(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = "Uptime",
                Color = Program.botColor
            };

            var connectUnixTime = ((DateTimeOffset)Program.connectTime).ToUnixTimeSeconds();

            var startTime = Convert.ToDateTime(Program.processStartTime);
            var startUnixTime = ((DateTimeOffset)startTime).ToUnixTimeSeconds();

            embed.AddField("Process started at", $"<t:{startUnixTime}:F> (<t:{startUnixTime}:R>)");
            embed.AddField("Last connected to Discord at", $"<t:{connectUnixTime}:F> (<t:{connectUnixTime}:R>)");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("timecheck",
            "[Authorized users only] Return the current time on the machine the bot is running on.")]
        public async Task TimeCheck(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().AddEmbed(new DiscordEmbedBuilder
            {
                Title = "Time Check", Color = Program.botColor,
                Description = $"Seems to me like it's currently `{DateTime.Now}`."
            }));
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

            DiscordEmbedBuilder embed = new()
            {
                Title = "Owners",
                Color = Program.botColor
            };

            List<DiscordUser> botOwners = new();
            List<DiscordUser> authorizedUsers = new();

            foreach (var owner in ctx.Client.CurrentApplication.Owners) botOwners.Add(owner);

            foreach (var userId in Program.configjson.Base.AuthorizedUsers)
                authorizedUsers.Add(await ctx.Client.GetUserAsync(Convert.ToUInt64(userId)));

            var botOwnerList = "";
            foreach (var owner in botOwners)
                botOwnerList += $"\n- {owner.Username}#{owner.Discriminator} (`{owner.Id}`)";

            var authUsersList = "";
            foreach (var user in authorizedUsers)
                authUsersList += $"\n- {user.Username}#{user.Discriminator} (`{user.Id}`)";

            embed.AddField("Bot Owners", botOwnerList);
            embed.AddField("Authorized Users",
                $"These users are authorized to use owner-level commands.\n{authUsersList}");

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("guilds", "[Authorized users only] Show the guilds that the bot is in.")]
        public async Task Guilds(InteractionContext ctx)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = $"Joined Guilds - {Program.discord.Guilds.Count}",
                Color = Program.botColor
            };

            foreach (var guild in Program.discord.Guilds)
                embed.Description += $"- `{guild.Value.Id}`: {guild.Value.Name}\n";

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("humandateparser",
            "[Authorized users only] See what happens when HumanDateParser tries to parse a date.")]
        public async Task HumanDateParserCmd(InteractionContext ctx,
            [Option("date", "The date (or time) for HumanDateParser to parse.")]
            string date)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            DiscordEmbedBuilder embed = new()
            {
                Title = "HumanDateParser Result",
                Color = Program.botColor
            };

            try
            {
                embed.WithDescription(
                    $"<t:{((DateTimeOffset)HumanDateParser.HumanDateParser.Parse(date)).ToUnixTimeSeconds()}:F>");
            }
            catch (ParseException ex)
            {
                embed.WithDescription($"{ex.Message}");
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().AddEmbed(embed));
        }

        [SlashCommand("checks", "[Authorized users only] Run the bot's timed checks manually.")]
        public async Task DebugChecks(InteractionContext ctx,
            [Option("checks", "The checks that should be run.")]
            [Choice("All", "all")]
            [Choice("Reminders", "reminders")]
            [Choice("Package Updates", "packageupdates")]
            string checksToRun)
        {
            await ctx.CreateResponseAsync(InteractionResponseType.DeferredChannelMessageWithSource);

            if (checksToRun == "all")
            {
                await ReminderChecks.ReminderCheck();
                await PackageUpdateChecks.PackageUpdateCheck();
            }
            else if (checksToRun == "reminders")
            {
                await ReminderChecks.ReminderCheck();
            }
            else if (checksToRun == "packageupdates")
            {
                await PackageUpdateChecks.PackageUpdateCheck();
            }

            await ctx.FollowUpAsync(new DiscordFollowupMessageBuilder().WithContent("Done!"));
        }
    }
}
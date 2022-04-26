using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using System;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
    public class ComplaintSlashCommands : ApplicationCommandModule
    {
        [SlashCommandGroup("complaint", "File a complaint to a specific department.")]
        public class Complaint
        {
            [SlashCommand("hr", "Send a complaint to HR.")]
            public async Task HrComplaint(InteractionContext ctx, [Option("complaint", "Your complaint.")] string complaint)
            {
                if (ctx.Guild.Id != 631118217384951808 && ctx.Guild.Id != Program.configjson.DevServerId)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("This command is not available in this server.").AsEphemeral(true));
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Your complaint has been recorded. You can see it below. You will be contacted soon about your issue.\n> {complaint}").AsEphemeral(true));
                DiscordChannel logChannel = await ctx.Client.GetChannelAsync(968515974271741962);
                await logChannel.SendMessageAsync($"{ctx.User.Mention} to HR:\n> {complaint}");
            }
            [SlashCommand("ia", "Send a complaint to IA.")]
            public async Task IaComplaint(InteractionContext ctx, [Option("complaint", "Your complaint.")] string complaint)
            {
                if (ctx.Guild.Id != 631118217384951808 && ctx.Guild.Id != Program.configjson.DevServerId)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("This command is not available in this server.").AsEphemeral(true));
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Your complaint has been recorded. You can see it below. You will be contacted soon about your issue.\n> {complaint}").AsEphemeral(true));
                DiscordChannel logChannel = await ctx.Client.GetChannelAsync(968515974271741962);
                await logChannel.SendMessageAsync($"{ctx.User.Mention} to IA:\n> {complaint}");
            }
            [SlashCommand("it", "Send a complaint to IT.")]
            public async Task ItComplaint(InteractionContext ctx, [Option("complaint", "Your complaint.")] string complaint)
            {
                if (ctx.Guild.Id != 631118217384951808 && ctx.Guild.Id != Program.configjson.DevServerId)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("This command is not available in this server.").AsEphemeral(true));
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Your complaint has been recorded. You can see it below. You will be contacted soon about your issue.\n> {complaint}").AsEphemeral(true));
                DiscordChannel logChannel = await ctx.Client.GetChannelAsync(968515974271741962);
                await logChannel.SendMessageAsync($"{ctx.User.Mention} to IT:\n> {complaint}");
            }
            [SlashCommand("corporate", "Send a complaint to corporate.")]
            public async Task CorporateComplaint(InteractionContext ctx, [Option("complaint", "Your complaint.")] string complaint)
            {
                if (ctx.Guild.Id != 631118217384951808 && ctx.Guild.Id != Program.configjson.DevServerId)
                {
                    await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent("This command is not available in this server.").AsEphemeral(true));
                }

                await ctx.CreateResponseAsync(new DiscordInteractionResponseBuilder().WithContent($"Your complaint has been recorded. You can see it below. You will be contacted soon about your issue.\n> {complaint}").AsEphemeral(true));
                DiscordChannel logChannel = await ctx.Client.GetChannelAsync(968515974271741962);
                await logChannel.SendMessageAsync($"{ctx.User.Mention} to Corporate:\n> {complaint}");
            }
        }
    }

    public class PerServerFeatures : BaseCommandModule
    {
        // Per-server commands go here. Use the [TargetServer(serverId)] attribute to restrict a command to a specific guild.

        // Note that this command here can be removed if another command is added; there just needs to be one here to prevent an exception from being thrown when the bot is run.
        [Command("dummycommand")]
        [Hidden]
        public async Task DummyCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("Hi! This command does nothing other than prevent an exception from being thrown when the bot is run. :)");
        }

        public static async Task WednesdayCheck()
        {
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] WednesdayCheck running.");
#endif
            if (DateTime.Now.DayOfWeek != DayOfWeek.Wednesday)
            {
                return;
            }
            else if (!DateTime.Now.ToShortTimeString().Contains("10:00"))
            {
                return;
            }

            try
            {
                DiscordChannel channel = await Program.discord.GetChannelAsync(874488354786394192);
                // await channel.SendMessageAsync("(this message will be changed at some point)");

            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred! Details: {e}");
                return;
            }
        }

        public static async Task PizzaTime()
        {
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] PizzaTime running.");
#endif
            if (!DateTime.Now.ToShortTimeString().Contains("12:00"))
            {
                return;
            }

            try
            {
                DiscordChannel channel = await Program.discord.GetChannelAsync(932768798224838778);
                await channel.SendMessageAsync("https://cdn.discordapp.com/attachments/932768798224838778/932768814284812298/IMG_9147.png");
            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred! Details: {e}");
                return;
            }
        }
    }

    public class TargetServerAttribute : CheckBaseAttribute
    {
        public ulong TargetGuild { get; private set; }

        public TargetServerAttribute(ulong targetGuild)
        {
            TargetGuild = targetGuild;
        }

        public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return !ctx.Channel.IsPrivate && ctx.Guild.Id == TargetGuild;
        }
    }
}

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
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

        public static async Task EsportsPing()
        {
#if DEBUG
            Console.WriteLine($"[{DateTime.Now}] EsportsCheck running.");
#endif
            if (!DateTime.Now.ToShortTimeString().Contains(":00"))
            {
                return;
            }

            try
            {
                DiscordChannel channel = await Program.discord.GetChannelAsync(935271047400407050);
                await channel.SendMessageAsync("<@567007649149878277> the smash trial is at 5. Be there Mr. petrazarki");
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

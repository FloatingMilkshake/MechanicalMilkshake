using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.Threading.Tasks;

namespace MechanicalMilkshake.Modules
{
    public class PerServerFeatures : BaseCommandModule
    {
        public static bool wedCheckDone = false;

        // per-server commands go here (use [TargetServer]!)
        [Command("checkserver")]
        public async Task CheckServer(CommandContext ctx, ulong id)
        {
            if (id == ctx.Guild.Id)
            {
                await ctx.RespondAsync("Check successful. This server's ID matches the ID you provided.");
            }
            else
            {
                await ctx.RespondAsync("Check failed! This server's ID does not match the ID you provided.");
            }
        }

        public static async Task WednesdayCheck()
        {
            DiscordChannel channel = null;
            if (DateTime.Now.DayOfWeek != DayOfWeek.Wednesday)
            {
                return;
            }
            if (DateTime.Now.DayOfWeek == DayOfWeek.Wednesday && wedCheckDone || DateTime.Now.ToShortTimeString() != "10:00 AM")
            {
                return;
            }

            try
            {
                channel = await Program.discord.GetChannelAsync(874488354786394192);
                await channel.SendMessageAsync("(this message will be changed at some point)");
                wedCheckDone = true;

            }
            catch (Exception e)
            {
                Console.WriteLine($"An error occurred! Details: {e}");
                return;
            }
        }
    }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
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
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
}

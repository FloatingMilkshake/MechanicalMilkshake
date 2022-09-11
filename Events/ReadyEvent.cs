namespace MechanicalMilkshake.Events;

public class ReadyEvent
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static async Task OnReady(DiscordClient client, ReadyEventArgs e)
    {
        Task.Run(async () =>
        {
            Program.connectTime = DateTime.Now;

            DiscordEmbedBuilder embed = new()
            {
                Title = "Connected!",
                Color = new DiscordColor("#FAA61A")
            };

            var debugInfo = DebugInfoHelper.GetDebugInfo();

            embed.AddField("Framework", debugInfo.Framework, true);
            embed.AddField("Platform", debugInfo.Platform, true);
            embed.AddField("Library", debugInfo.Library, true);
            embed.AddField("Commit Hash", $"`{debugInfo.CommitHash}`", true);
            embed.AddField(debugInfo.CommitTimeDescription, debugInfo.CommitTimestamp, true);
            embed.AddField("Commit Message", debugInfo.CommitMessage);

            embed.AddField("Server Count", client.Guilds.Count.ToString(), true);

            int commandCount;
#if DEBUG
            commandCount = (await Program.discord.GetGuildApplicationCommandsAsync(Program.configjson.HomeServerId))
                .Count;
#else
        commandCount = (await Program.discord.GetGlobalApplicationCommandsAsync()).Count;
#endif
            embed.AddField("Command Count", commandCount.ToString(), true);
            embed.AddField("Load Time", debugInfo.LoadTime, true);

            await Program.homeChannel.SendMessageAsync(embed);

            //await Program.homeChannel.SendMessageAsync($"Connected!\n{DebugInfoHelper.GetDebugInfo()}");
        });
    }
}
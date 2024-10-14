namespace MechanicalMilkshake.Events;

public class ReadyEvent
{
    public static async Task OnReady(DiscordClient client, SessionCreatedEventArgs e)
    {
        await Task.Run(async () =>
        {
            Program.ConnectTime = DateTime.Now;

            await Program.HomeChannel.SendMessageAsync(await DebugInfoHelpers.GenerateDebugInfoEmbed(true));

            await ActivityTasks.SetActivityAsync();
        });
    }
}
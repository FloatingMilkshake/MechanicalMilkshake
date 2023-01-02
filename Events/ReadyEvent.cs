namespace MechanicalMilkshake.Events;

public class ReadyEvent
{
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
    public static Task OnReady(DiscordClient client, ReadyEventArgs e)
    {
        Task.Run(async () =>
        {
            Program.ConnectTime = DateTime.Now;

            await Program.HomeChannel.SendMessageAsync(await DebugInfoHelpers.GenerateDebugInfoEmbed(true));

            await CustomStatusHelpers.SetCustomStatus();
        });
        return Task.CompletedTask;
    }
}
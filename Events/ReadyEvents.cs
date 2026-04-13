namespace MechanicalMilkshake.Events;

internal class ReadyEvents
{
    internal static async Task HandleReadyEventAsync(DiscordClient _, SessionCreatedEventArgs __)
    {
        Setup.State.Discord.ConnectTime = DateTime.Now;
    }
}

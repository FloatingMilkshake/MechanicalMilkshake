namespace MechanicalMilkshake;

internal class GatewayController : IGatewayController
{
    public async Task HeartbeatedAsync(IGatewayClient client) => await HeartbeatEvent.HandleHeartbeatEventAsync(client);
    public async Task ResumeAttemptedAsync(IGatewayClient _) { }
    public async Task ZombiedAsync(IGatewayClient _) { }
    public async Task ReconnectRequestedAsync(IGatewayClient _) { }
    public async Task ReconnectFailedAsync(IGatewayClient _) { }
    public async Task SessionInvalidatedAsync(IGatewayClient _) { }
}
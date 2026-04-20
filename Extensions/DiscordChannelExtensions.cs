namespace MechanicalMilkshake.Extensions;

internal static class DiscordChannelExtensions
{
    extension(DiscordChannel channel)
    {
        internal bool HasHighRatelimitRisk()
        {
            if (Setup.State.Process.Configuration.RatelimitCautionChannels.Count == 0)
                return false;

            if (Setup.State.Process.Configuration.RatelimitCautionChannels.Contains(channel.Id.ToString()))
                return true;

            if (channel.ParentId is not null && Setup.State.Process.Configuration.RatelimitCautionChannels.Contains(channel.ParentId.ToString()))
                return true;

            return false;
        }
    }
}

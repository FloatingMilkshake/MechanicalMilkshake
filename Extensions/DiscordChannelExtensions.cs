namespace MechanicalMilkshake.Extensions;

internal static class DiscordChannelExtensions
{
    extension(DiscordChannel channel)
    {
        internal bool HasHighRatelimitRisk()
        {
            if (Setup.Configuration.ConfigJson.RatelimitCautionChannels.Count == 0)
                return false;

            if (Setup.Configuration.ConfigJson.RatelimitCautionChannels.Contains(channel.Id.ToString()))
                return true;

            if (channel.ParentId is not null && Setup.Configuration.ConfigJson.RatelimitCautionChannels.Contains(channel.ParentId.ToString()))
                return true;

            return false;
        }
    }
}

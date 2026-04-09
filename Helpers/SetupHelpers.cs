namespace MechanicalMilkshake.Helpers;

internal class SetupHelpers
{
    internal static async Task CheckConfigurationAsync()
    {
        try
        {
            Setup.Configuration.Discord.HomeServer =
                await Setup.State.Discord.Client.GetGuildAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.HomeServer));
            Setup.Configuration.Discord.Channels.Home =
                await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.HomeChannel));
        }
        catch
        {
            Setup.State.Discord.Client.Logger.LogCritical("\"homeChannel\" or \"homeServer\" in config.json are misconfigured. Please make sure you have a valid ID for both of these values.");
            Environment.Exit(1);
        }

        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.FeedbackChannel))
            try
            {
                Setup.Configuration.Discord.Channels.Feedback =
                    await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.FeedbackChannel));
            }
            catch
            {
                Setup.State.Discord.Client.Logger.LogWarning("Feedback command disabled due to invalid or missing channel ID.");
            }

        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.GuildLogChannel))
            try
            {
                Setup.Configuration.Discord.Channels.GuildLogs =
                    await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.GuildLogChannel));
            }
            catch
            {
                Setup.State.Discord.Client.Logger.LogWarning("Guild join/leave logs disabled due to invalid or missing channel ID.");
            }

        if (!string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.SlashCommandLogChannel))
            try
            {
                Setup.Configuration.Discord.Channels.CommandLogs =
                    await Setup.State.Discord.Client.GetChannelAsync(Convert.ToUInt64(Setup.Configuration.ConfigJson.SlashCommandLogChannel));
            }
            catch
            {
                Setup.State.Discord.Client.Logger.LogWarning("Interaction command logs disabled due to invalid or missing channel ID.");
            }

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.WolframAlphaAppId))
        {
            Setup.State.Discord.Client.Logger.LogWarning("WolframAlpha commands disabled due to missing App ID.");
        }

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.UptimeKumaHeartbeatUrl))
        {
            Setup.State.Discord.Client.Logger.LogWarning("Uptime Kuma heartbeats disabled due to missing push URL.");
        }

        if (string.IsNullOrWhiteSpace(Setup.Configuration.ConfigJson.DbotsApiToken))
        {
            Setup.State.Discord.Client.Logger.LogWarning("DBots stats posting disabled due to missing configuration.");
        }
    }
}

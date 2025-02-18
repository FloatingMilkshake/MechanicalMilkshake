namespace MechanicalMilkshake.Events;

public class GuildMemberEvents
{
    public static async Task GuildMemberUpdated(DiscordClient _, GuildMemberUpdatedEventArgs e)
    {
        if (Program.ConfigJson.Base.UseServerSpecificFeatures) await ServerSpecificFeatures.EventChecks.GuildMemberUpdateChecks(e);
    }
}
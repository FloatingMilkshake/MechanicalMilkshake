namespace MechanicalMilkshake.Refs;

public class ModRoleConfig
{
    [JsonProperty("guildId")] public ulong GuildId { get; set; }
        
    [JsonProperty("adminRoleId")] public ulong AdminRoleId { get; set; }
        
    [JsonProperty("modRoleId")] public ulong ModRoleId { get; set; }

    [JsonProperty("trialModRoleId")] public ulong TrialModRoleId { get; set; }
        
}
namespace MechanicalMilkshake.Helpers;

public class ModRoleHelpers
{
    public static async Task<bool> UserHasModRole(DiscordMember member, DiscordGuild guild)
    {
        if (member.Permissions.HasPermission(Permissions.Administrator))
            return true;

        var modRolesSerialized = await Program.Db.HashGetAsync("modroles", guild.Id);
        if (!modRolesSerialized.HasValue) return false;
        var modRoles = JsonConvert.DeserializeObject<ModRoleConfig>(modRolesSerialized);

        return member.Roles.Any(r => r.Id == modRoles!.AdminRoleId || r.Id == modRoles!.ModRoleId || r.Id == modRoles!.TrialModRoleId);
    }
}
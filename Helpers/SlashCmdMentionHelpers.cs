namespace MechanicalMilkshake.Helpers;

public class SlashCmdMentionHelpers
{
    public static string GetSlashCmdMention(string cmdName, string subCommand = default, string subSubCommand = default)
    {
        if (cmdName.Contains(' '))
        {
            var split = cmdName.Split(' ');
            cmdName = split[0];
            switch (split.Length)
            {
                case 2:
                    subCommand = split[1];
                    break;
                case 3:
                    subCommand = split[1];
                    subSubCommand = split[2];
                    break;
            }
        }

        cmdName = cmdName.Trim();
        subCommand = subCommand?.Trim();
        subSubCommand = subSubCommand?.Trim();

        if (Program.ApplicationCommands is null) return $"`/{cmdName}`";

        var cmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == cmdName);
        if (cmd is null) return $"`/{cmdName}`";

        if (subCommand != default)
            return subSubCommand != default
                ? $"</{cmd.Name} {subCommand} {subSubCommand}:{cmd.Id}>"
                : $"</{cmd.Name} {subCommand}:{cmd.Id}>";
        return $"</{cmd.Name}:{cmd.Id}>";
    }
}
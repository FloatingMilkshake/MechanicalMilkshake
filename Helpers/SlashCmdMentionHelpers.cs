namespace MechanicalMilkshake.Helpers;

public class SlashCmdMentionHelpers
{
    public static string GetSlashCmdMention(string cmdName, string subCommand = default, string subSubCommand = default)
    {
        if (char.IsUpper(cmdName[0]) && subCommand == default && subSubCommand == default)
        {
            // This is probably a context menu command.
            // Return it in inline code instead of a command mention.

            return $"`{cmdName}`";
        }

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

        if (Program.ApplicationCommands is null)
            return subCommand != default
                ? subSubCommand != default
                    ? $"`/{cmdName} {subCommand} {subSubCommand}`"
                    : $"{cmdName} {subCommand}"
                : $"{cmdName}";

        var cmd = Program.ApplicationCommands.FirstOrDefault(c => c.Name == cmdName);

        if (cmd is null)
            return subCommand != default
                ? subSubCommand != default
                    ? $"`/{cmdName} {subCommand} {subSubCommand}`"
                    : $"`/{cmdName} {subCommand}`"
                : $"`/{cmdName}`";

        if (subCommand != default)
            return subSubCommand != default
                ? $"</{cmd.Name} {subCommand} {subSubCommand}:{cmd.Id}>"
                : $"</{cmd.Name} {subCommand}:{cmd.Id}>";
        return $"</{cmd.Name}:{cmd.Id}>";
    }
}
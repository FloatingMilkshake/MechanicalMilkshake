namespace MechanicalMilkshake.Extensions;

internal static class CommandExtensions
{
    extension(Command command)
    {
        internal string GetSlashCommandMention()
        {
            return command.FullName.AsSlashCommandMention();
        }
    }
}

namespace MechanicalMilkshake.Extensions;

internal static class StringExtensions
{
    extension(string str)
    {
        internal string AsSlashCommandMention()
        {
            if (char.IsUpper(str[0]) && !str.Contains(' '))
                // This is probably a context menu command.
                // Return it in inline code instead of a command mention.
                return $"`{str}`";

            if (Setup.State.Discord.ApplicationCommands is null)
                return $"`{string.Join(' ', str)}`";

            var command = Setup.State.Discord.ApplicationCommands.FirstOrDefault(c => c.Name == str.Split(' ').First());

            if (command is null)
                return $"`/{string.Join(' ', str)}`";

            return $"</{string.Join(' ', str)}:{command.Id}>";
        }

        internal List<string> SplitForDiscord(int maxLength = 1980)
        {
            List<string> split = [];

            if (str.Length > maxLength)
            {
                // Split by lines
                // If adding a line to the current element in `split` does not put it over `maxLength`, do so
                // If it does put it over `maxLength`, move on to the next element
                var currentElement = "";
                var lines = str.Split('\n');
                string codeBlockStart = null;

                // If input is a code block, record the start line (backticks & language) for later & remove it & the last line (ending backticks) from the list
                var codeBlockRegex = "```.*$";
                var match = Regex.Match(lines.First(), codeBlockRegex);
                if (match.Success)
                {
                    codeBlockStart = match.Value;
                    lines = lines.Skip(1).Take(lines.Length - 2).ToArray();
                }

                foreach (var line in lines)
                {
                    if (currentElement.Length + line.Length + 20 < maxLength) // + 20 for newline & extra space for code block start
                    {
                        currentElement += line + '\n';
                    }
                    else
                    {
                        split.Add(codeBlockStart is null
                            ? currentElement
                            : $"{codeBlockStart}\n{currentElement}```");
                        currentElement = line + '\n';
                    }
                }

                // `currentElement` is left with a final group of lines in it; add it to `split` too
                split.Add(codeBlockStart is null
                    ? currentElement
                    : $"{codeBlockStart}\n{currentElement}```");
            }
            else
            {
                split.Add(str);
            }

            return split;
        }
    }
}

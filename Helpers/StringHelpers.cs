namespace MechanicalMilkshake.Helpers;

internal class StringHelpers
{
    public static async Task<List<string>> SplitStringAsync(string input, bool respond = false, int maxLength = 1980, CommandContext ctx = null, string completionMessage = null)
    {
        List<string> split = [];

        if (input.Length > maxLength)
        {
            // Split by lines
            // If adding a line to the current element in `split` does not put it over `maxLength`, do so
            // If it does put it over `maxLength`, move on to the next element
            var currentElement = "";
            var lines = input.Split('\n');
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
            split.Add(input);
        }

        if (!respond)
            return split;

        if (ctx is null)
            throw new ArgumentException("SplitStringAsync cannot respond to a null CommandContext. Please provide a CommandContext.");

        foreach (var message in split)
        {
            await ctx.Channel.SendMessageAsync(message);
            await Task.Delay(2000);
        }
        if (completionMessage is not null)
            await ctx.RespondAsync(completionMessage);

        return [];
    }

    public static string Truncate(string input, int maxLength)
    {
        if (input is null && maxLength <= 9)
            return "[invalid]";
        else if (input is null)
            return "";

        if (input.Length < maxLength)
            return input;

        return input[..maxLength] + "...";
    }
}

namespace MechanicalMilkshake.Helpers;

public static class MarkdownParser
{
    /*
     * This parses a string and escapes all the markdown characters.
     */
    public static string Parse(string input)
    {
        var output = input;
        // If the string has a link, skip it.
        if (output.Contains("]("))
        {
            var link = output.Substring(output.IndexOf("](", StringComparison.Ordinal) + 2);
            link = link.Substring(0, link.IndexOf(")", StringComparison.Ordinal));
            output = output.Replace(link, "");
        }
        
        output = output.Replace("\\", "\\\\");
        output = output.Replace("`", @"\`");
        output = output.Replace("*", @"\*");
        output = output.Replace("_", @"\_");
        output = output.Replace("~", @"\~");
        output = output.Replace(">", @"\>");
        output = output.Replace("[", @"\[");
        output = output.Replace("]", @"\]");
        output = output.Replace("(", @"\(");
        output = output.Replace(")", @"\)");
        output = output.Replace("#", @"\#");
        output = output.Replace("+", @"\+");
        output = output.Replace("-", @"\-");
        output = output.Replace("=", @"\=");
        output = output.Replace("|", @"\|");
        output = output.Replace("{", @"\{");
        output = output.Replace("}", @"\}");
        
        return output;
    }
}
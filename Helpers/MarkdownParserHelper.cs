namespace MechanicalMilkshake.Helpers;

public static class MarkdownParser
{
    /*
     * This parses a string and escapes all the markdown characters.
     */
    public static string Parse(string input)
    {
        var output = input;
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
        
        // if output becomes greater than 4000 characters, return an error.
        if (output.Length > 4000)
        {
            return "The output is too long for me to send.";
        }
        
        return output;
    }
}
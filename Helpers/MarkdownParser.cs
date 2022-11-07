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
        output = output.Replace(".", @"\.");
        output = output.Replace("!", @"\!");
        
        return output;
    }
}
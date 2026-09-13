using System.Text;

namespace Wctl.Scenes;

/// <summary>Splits one step into words the way a shell would: spaces separate, double quotes group.</summary>
public static class StepLine
{
    public static string[] Split(string line)
    {
        var words = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;
        var hasWord = false;

        foreach (var c in line)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
                hasWord = true;
                continue;
            }

            if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (hasWord)
                {
                    words.Add(current.ToString());
                    current.Clear();
                    hasWord = false;
                }

                continue;
            }

            current.Append(c);
            hasWord = true;
        }

        if (inQuotes)
        {
            throw new WctlException($"A quote is not closed in '{line}'.");
        }

        if (hasWord)
        {
            words.Add(current.ToString());
        }

        return words.ToArray();
    }
}

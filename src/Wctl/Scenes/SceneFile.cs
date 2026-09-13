using System.Text;

namespace Wctl.Scenes;

/// <summary>
/// The scenes file: a [name] header starts a scene, every following line is a step exactly as typed after "w",
/// '#' starts a comment. Reading and writing are pure functions over text so they can be tested.
/// </summary>
public static class SceneFile
{
    private const string Header =
        "# wctl scenes. One scene per [name] block, one command per line, exactly as typed after 'w'.\n"
        + "# Run one with: w scene <name>   or simply: w <name>\n";

    public static List<Scene> Parse(string text)
    {
        var scenes = new List<Scene>();
        string? name = null;
        var steps = new List<string>();
        var lineNumber = 0;

        foreach (var raw in text.Split('\n'))
        {
            lineNumber++;
            var line = raw.Trim();
            if (line.Length == 0 || line[0] == '#')
            {
                continue;
            }

            if (line[0] == '[')
            {
                if (line[^1] != ']')
                {
                    throw new WctlException($"Scenes file line {lineNumber}: a scene header looks like [name]. Got '{line}'.");
                }

                Flush();
                name = line[1..^1].Trim();
                ValidateName(name, $"Scenes file line {lineNumber}");
                if (scenes.Exists(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
                {
                    throw new WctlException($"Scenes file line {lineNumber}: the scene '{name}' appears twice.");
                }

                continue;
            }

            if (name is null)
            {
                throw new WctlException($"Scenes file line {lineNumber}: '{line}' comes before any [scene] header.");
            }

            steps.Add(line);
        }

        Flush();
        return scenes;

        void Flush()
        {
            if (name is not null)
            {
                scenes.Add(new Scene(name, steps.ToList()));
                steps.Clear();
            }
        }
    }

    public static string Format(IEnumerable<Scene> scenes)
    {
        var sb = new StringBuilder(Header);
        foreach (var scene in scenes)
        {
            sb.Append('\n').Append('[').Append(scene.Name).Append("]\n");
            foreach (var step in scene.Steps)
            {
                sb.Append(step).Append('\n');
            }
        }

        return sb.ToString();
    }

    /// <summary>Names are typed as commands, so they must be one plain word.</summary>
    public static void ValidateName(string name, string where = "Scene name")
    {
        if (name.Length == 0 || !name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_'))
        {
            throw new WctlException($"{where}: a scene name is one word of letters, digits, '-' or '_'. Got '{name}'.");
        }
    }
}

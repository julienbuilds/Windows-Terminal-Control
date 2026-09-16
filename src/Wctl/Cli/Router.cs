using Wctl.Platform;
using Wctl.Scenes;

namespace Wctl.Cli;

/// <summary>Picks the command for a parsed command line and runs it.</summary>
public static class Router
{
    /// <param name="sceneFallback">Whether an unknown first word may run a scene of that name. Off while a scene runs, so scenes cannot nest.</param>
    public static int Dispatch(ParsedArgs parsed, CommandTable table, Services services, IOutput output, TextWriter stdout, bool sceneFallback = true)
    {
        if (parsed.Version)
        {
            output.State("w", VersionInfo.Current);
            return ExitCodes.Ok;
        }

        if (parsed.Positionals.Count == 0)
        {
            return Help.Overview(table, output);
        }

        var word = parsed.Positionals[0];
        IReadOnlyList<string> rest = parsed.Positionals.Skip(1).ToList();

        var command = table.Find(word);
        if (command is null)
        {
            // Unknown first word: a scene of that name, otherwise something to open. "w work" runs the scene, "w spotify" opens the app.
            if (sceneFallback && table.Find("scene") is { } scene && SceneExists(services, word))
            {
                command = scene;
                rest = [word];
            }
            else
            {
                command = table.Find("open")
                    ?? throw new WctlException($"Unknown command '{word}'. Run 'w help' to see all commands.");
                rest = parsed.Positionals;
            }
        }

        if (parsed.Help)
        {
            return Help.Command(command, output);
        }

        if (rest.Count > command.MaxArgs)
        {
            throw new WctlException($"Too many arguments for '{command.Name}'. Usage: w {command.Usage}");
        }

        return command.Run(new Invocation(rest, parsed, output, stdout, table, services));
    }

    private static bool SceneExists(Services services, string name)
    {
        var text = services.Scenes.Read();
        if (text is null)
        {
            return false;
        }

        try
        {
            return SceneFile.Parse(text).Exists(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
        catch (WctlException)
        {
            // A broken scenes file is reported by 'w scene list' with the line number. It must not break every other command.
            return false;
        }
    }
}

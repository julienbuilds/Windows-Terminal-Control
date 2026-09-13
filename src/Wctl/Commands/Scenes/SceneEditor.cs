using System.Globalization;
using Wctl.Cli;
using Wctl.Scenes;

namespace Wctl.Commands.Scenes;

/// <summary>
/// The review loop for a scene: shows the numbered steps and takes short editor commands until the user saves or quits.
/// </summary>
internal sealed class SceneEditor(Invocation inv)
{
    private const string Commands = "r <n> remove   m <n> <to> move   a <step> add   t <n> test   s save   q quit";

    public int Run(string name, List<string> steps)
    {
        if (inv.Options.Json)
        {
            throw new WctlException("The scene editor is interactive and has no --json mode.");
        }

        var output = inv.Output;
        while (true)
        {
            Show(name, steps);
            var line = inv.Services.Prompt.ReadLine("> ");
            if (line is null)
            {
                output.Message("Input ended. Not saved.");
                return ExitCodes.Failure;
            }

            string[] words;
            try
            {
                words = StepLine.Split(line);
            }
            catch (WctlException e)
            {
                output.Message(e.Message);
                continue;
            }

            if (words.Length == 0)
            {
                continue;
            }

            switch (words[0].ToLowerInvariant())
            {
                case "r" or "remove":
                    if (Index(words, 1, steps.Count) is int remove)
                    {
                        steps.RemoveAt(remove);
                    }

                    break;

                case "m" or "move":
                    if (Index(words, 1, steps.Count) is int from && Index(words, 2, steps.Count) is int to)
                    {
                        var moved = steps[from];
                        steps.RemoveAt(from);
                        steps.Insert(to, moved);
                    }

                    break;

                case "a" or "add":
                    var step = line.TrimStart()[words[0].Length..].Trim();
                    if (Accept(step))
                    {
                        steps.Add(step);
                    }

                    break;

                case "t" or "test":
                    if (Index(words, 1, steps.Count) is int test)
                    {
                        var (ok, result) = SceneRunner.RunStep(steps[test], inv, null);
                        output.StepResult(steps[test], ok, result);
                    }

                    break;

                case "s" or "save":
                    Save(name, steps);
                    output.State("Saved", $"{name} ({steps.Count} steps)");
                    return ExitCodes.Ok;

                case "q" or "quit":
                    output.Message("Not saved.");
                    return ExitCodes.Ok;

                case "?" or "help":
                    output.Message(Commands);
                    break;

                default:
                    output.Message($"Unknown editor command '{words[0]}'. {Commands}");
                    break;
            }
        }
    }

    private void Show(string name, List<string> steps)
    {
        var output = inv.Output;
        output.Message(string.Empty);
        output.Message(steps.Count == 0 ? $"Scene {name}: no steps yet." : $"Scene {name}:");
        for (var i = 0; i < steps.Count; i++)
        {
            output.Message($"  {i + 1,3}  {steps[i]}");
        }

        output.Message(string.Empty);
        output.Message(Commands);
    }

    /// <summary>A 1 based step number from the editor command, or null with an explanation.</summary>
    private int? Index(string[] words, int at, int count)
    {
        if (at < words.Length
            && int.TryParse(words[at], NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            && number >= 1 && number <= count)
        {
            return number - 1;
        }

        inv.Output.Message(count == 0 ? "There are no steps yet." : $"Which step? A number from 1 to {count}.");
        return null;
    }

    private bool Accept(string step)
    {
        if (step.Length == 0)
        {
            inv.Output.Message("Add what? Example: a vol 20");
            return false;
        }

        var first = StepLine.Split(step)[0];
        var command = inv.Table.Find(first);
        if (command is not null && command == inv.Table.Find("scene"))
        {
            inv.Output.Message("A scene cannot run another scene.");
            return false;
        }

        if (command is null)
        {
            inv.Output.Message($"'{first}' is not a command, so this step will open an app called {first}.");
        }

        return true;
    }

    private void Save(string name, List<string> steps)
    {
        var store = inv.Services.Scenes;
        var scenes = SceneCommand.Load(store);
        scenes.RemoveAll(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        scenes.Add(new Scene(name, steps.ToList()));
        store.Write(SceneFile.Format(scenes));
    }
}

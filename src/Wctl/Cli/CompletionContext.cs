using System.Globalization;
using Wctl.Commands.Apps;
using Wctl.Platform;
using Wctl.Scenes;

namespace Wctl.Cli;

/// <summary>What a command's completer gets: the words typed after its name, and the ways to look up live candidates.</summary>
internal sealed class CompletionContext
{
    /// <summary>The words after the command name. The last one is what the user is typing, and may be empty.</summary>
    public required IReadOnlyList<string> Words { get; init; }

    public required Services Services { get; init; }

    public required CommandTable Table { get; init; }

    public required AppCatalog Catalog { get; init; }

    /// <summary>Which argument is being typed. 0 is the first word after the command name.</summary>
    public int Position => Words.Count - 1;

    /// <summary>The word at that position, or an empty string when there is none.</summary>
    public string Word(int index) => index >= 0 && index < Words.Count ? Words[index] : string.Empty;

    public IEnumerable<string> AppNames() => Catalog.Apps.Select(a => a.Name).Order(StringComparer.OrdinalIgnoreCase);

    /// <summary>App names of open windows, then the numbers 'w ls' shows.</summary>
    public IEnumerable<string> WindowTargets()
    {
        var windows = Services.Windows.List();
        return windows.Select(w => w.App)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Concat(Enumerable.Range(1, windows.Count).Select(Number));
    }

    public IEnumerable<string> AudioDevices() => Services.Audio.GetOutputDevices().Select(d => d.Name);

    public IEnumerable<string> SceneNames()
    {
        try
        {
            return SceneFile.Parse(Services.Scenes.Read() ?? string.Empty).Select(s => s.Name);
        }
        catch (WctlException)
        {
            // A broken scenes file is reported by 'w scene list' with the line number. It must not break completion.
            return [];
        }
    }

    /// <summary>Handles the shared "monitor &lt;m&gt;" option: the monitors after the word, the word itself before it.</summary>
    public IEnumerable<string> WithMonitorOption(params string[] own)
        => Word(Position - 1) is "monitor" or "mon"
            ? Services.Windows.Monitors().Select(m => Number(m.Index)).Prepend("main")
            : own.Append("monitor");

    private static string Number(int value) => value.ToString(CultureInfo.InvariantCulture);
}

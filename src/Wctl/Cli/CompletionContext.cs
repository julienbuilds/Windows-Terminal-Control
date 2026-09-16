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

    public IEnumerable<Candidate> AppNames() => Catalog.Apps
        .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
        .Select(a => new Candidate(a.Name, a.Executable.Length > 0 ? $"app, {a.Executable}.exe" : "app"));

    /// <summary>App names of open windows, then the numbers 'w ls' shows.</summary>
    public IEnumerable<Candidate> WindowTargets()
    {
        var windows = Services.Windows.List();

        var byApp = windows
            .GroupBy(w => w.App, StringComparer.OrdinalIgnoreCase)
            .Select(g => new Candidate(g.Key, g.Count() == 1 ? Shorten(g.First().Title) : $"{g.Count()} windows"));

        var byNumber = windows.Select((w, i) => new Candidate(
            (i + 1).ToString(CultureInfo.InvariantCulture),
            $"{w.App}: {Shorten(w.Title)}"));

        return byApp.Concat(byNumber);
    }

    public IEnumerable<Candidate> AudioDevices()
    {
        var current = Services.Audio.GetDefaultOutputId();
        return Services.Audio.GetOutputDevices()
            .Select(d => new Candidate(d.Name, d.Id == current ? "current output" : "audio output"));
    }

    public IEnumerable<Candidate> SceneNames()
    {
        try
        {
            return SceneFile.Parse(Services.Scenes.Read() ?? string.Empty)
                .Select(s => new Candidate(s.Name, $"scene, {s.Steps.Count} steps"))
                .ToList();
        }
        catch (WctlException)
        {
            // A broken scenes file is reported by 'w scene list' with the line number. It must not break completion.
            return [];
        }
    }

    /// <summary>Handles the shared "monitor &lt;m&gt;" option: the monitors after the word, the word itself before it.</summary>
    public IEnumerable<Candidate> WithMonitorOption(params Candidate[] own)
        => Word(Position - 1) is "monitor" or "mon"
            ? Monitors()
            : own.Append(new Candidate("monitor", "pick one monitor"));

    public IEnumerable<Candidate> Monitors()
    {
        var monitors = Services.Windows.Monitors();
        return monitors
            .Select(m => new Candidate(
                m.Index.ToString(CultureInfo.InvariantCulture),
                $"{m.Bounds.Width}x{m.Bounds.Height}{(m.Primary ? ", primary" : string.Empty)}"))
            .Prepend(new Candidate("main", "the primary monitor"));
    }

    private static string Shorten(string title) => title.Length <= 40 ? title : title[..39] + "…";
}

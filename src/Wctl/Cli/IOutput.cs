namespace Wctl.Cli;

/// <summary>
/// Where command results go. Commands only talk to this interface, so the same command
/// renders as colored text in a terminal or as one JSON object for scripts.
/// </summary>
public interface IOutput
{
    /// <summary>
    /// One fact. Terminal: "Label: value". JSON: a property named after the label ("Output device" becomes "output_device").
    /// <paramref name="value"/> is the typed value (bool, int, string). <paramref name="display"/> overrides the terminal text, for example "47%".
    /// </summary>
    void State(string label, object value, string? display = null);

    /// <summary>A fact for scripts only. Included in JSON output, not shown in the terminal, because the terminal line already says it.</summary>
    void Detail(string label, object value);

    /// <summary>A plain line of text for people. Not included in JSON output.</summary>
    void Message(string text);

    /// <summary>A list. Terminal: a table. JSON: an array of objects under <paramref name="key"/>, with column names as property names.</summary>
    void Table(string key, string[] columns, IReadOnlyList<string[]> rows, TableOptions? options = null);

    /// <summary>An error message. Terminal: red text on stderr. JSON: an "error" property.</summary>
    void Fail(string text);

    /// <summary>Called once when the command is done. JSON output is written here.</summary>
    void Flush();
}

/// <param name="Title">Section title above the table.</param>
/// <param name="Header">Whether to show the column names in the terminal.</param>
/// <param name="CurrentRow">Index of the row that is "current" (for example the active audio device), or -1.</param>
public sealed record TableOptions(string? Title = null, bool Header = true, int CurrentRow = -1);

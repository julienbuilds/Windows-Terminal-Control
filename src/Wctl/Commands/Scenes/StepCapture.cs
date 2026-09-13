using Wctl.Cli;

namespace Wctl.Commands.Scenes;

/// <summary>Collects what a command printed, so a scene can show it as one short result next to the step.</summary>
internal sealed class StepCapture : IOutput
{
    private readonly List<string> lines = [];

    public string Text => string.Join("; ", lines);

    public void State(string label, object value, string? display = null)
        => lines.Add($"{label}: {display ?? ConsoleOutput.FormatValue(value)}");

    public void Detail(string label, object value)
    {
    }

    public void Action(string target, string result) => lines.Add($"{target}: {result}");

    public void Message(string text)
    {
        if (text.Length > 0)
        {
            lines.Add(text);
        }
    }

    public void Status(string text)
    {
    }

    public void Table(string key, string[] columns, IReadOnlyList<string[]> rows, TableOptions? options = null)
        => lines.Add($"{rows.Count} {key}");

    public void StepResult(string command, bool ok, string result) => lines.Add($"{(ok ? "✓" : "✗")} {command}");

    public void Fail(string text) => lines.Add(text);

    public void Flush()
    {
    }
}

using System.Globalization;
using Spectre.Console;

namespace Wctl.Cli;

/// <summary>Output for people: short colored lines, tables for lists, red errors on stderr.</summary>
public sealed class ConsoleOutput(IAnsiConsole stdout, IAnsiConsole stderr) : IOutput
{
    public void State(string label, object value, string? display = null)
    {
        var text = display ?? FormatValue(value);
        stdout.MarkupLine($"[grey]{Markup.Escape(label)}:[/] [{ColorFor(text)}]{Markup.Escape(text)}[/]");
    }

    public void Detail(string label, object value)
    {
        // Details are for JSON. The terminal line from State already carries the information.
    }

    public void Action(string target, string result)
        => stdout.MarkupLine($"[bold]{Markup.Escape(target)}:[/] {Markup.Escape(result)}");

    public void Message(string text) => stdout.MarkupLine(Markup.Escape(text));

    public void Status(string text)
    {
        // Carriage return without line feed rewrites the same terminal line. Padding wipes what the last status left behind.
        var padding = Math.Max(0, statusLength - text.Length);
        stdout.Profile.Out.Writer.Write('\r' + text + new string(' ', padding) + (text.Length == 0 ? "\r" : string.Empty));
        stdout.Profile.Out.Writer.Flush();
        statusLength = text.Length;
    }

    private int statusLength;

    public void Table(string key, string[] columns, IReadOnlyList<string[]> rows, TableOptions? options = null)
    {
        if (rows.Count == 0)
        {
            // An empty table is noise. Commands print a message for the empty case.
            return;
        }

        options ??= new TableOptions();

        if (options.Title is not null)
        {
            stdout.MarkupLine($"[bold]{Markup.Escape(options.Title)}[/]");
        }

        var table = new Table();
        if (options.Header)
        {
            table.Border(TableBorder.Rounded).BorderColor(Color.Grey);
        }
        else
        {
            table.Border(TableBorder.None).HideHeaders();
        }

        var marker = options.CurrentRow >= 0;
        if (marker)
        {
            table.AddColumn(new TableColumn(" ").NoWrap());
        }

        foreach (var column in columns)
        {
            table.AddColumn(new TableColumn($"[grey]{Markup.Escape(column)}[/]"));
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var current = i == options.CurrentRow;
            IEnumerable<string> cells = rows[i].Select(cell => current ? $"[green]{Markup.Escape(cell)}[/]" : Markup.Escape(cell));
            if (marker)
            {
                cells = cells.Prepend(current ? "[green]●[/]" : " ");
            }

            table.AddRow(cells.ToArray());
        }

        stdout.Write(table);
    }

    public void StepResult(string command, bool ok, string result)
    {
        var mark = ok ? "[green]✓[/]" : "[red]✗[/]";
        var color = ok ? "grey" : "red";
        stdout.MarkupLine($"  {mark} {Markup.Escape(command.PadRight(30))} [{color}]{Markup.Escape(result)}[/]");
    }

    public void Fail(string text) => stderr.MarkupLine($"[red]error:[/] {Markup.Escape(text)}");

    public void Flush()
    {
    }

    internal static string FormatValue(object value) => value switch
    {
        bool b => b ? "on" : "off",
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static string ColorFor(string text) => text switch
    {
        "on" or "unmuted" => "green",
        "off" or "muted" => "yellow",
        _ => "bold",
    };
}

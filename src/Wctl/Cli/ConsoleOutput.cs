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

    public void Message(string text) => stdout.MarkupLine(Markup.Escape(text));

    public void Table(string key, string[] columns, IReadOnlyList<string[]> rows, TableOptions? options = null)
    {
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

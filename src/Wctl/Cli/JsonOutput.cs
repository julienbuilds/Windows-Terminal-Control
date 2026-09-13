using System.Text;
using System.Text.Json;

namespace Wctl.Cli;

/// <summary>Output for scripts: everything is collected and written as one JSON object when the command finishes.</summary>
public sealed class JsonOutput(TextWriter stdout) : IOutput
{
    private readonly List<KeyValuePair<string, object>> facts = [];
    private readonly List<(string Key, string[] Columns, IReadOnlyList<string[]> Rows, int CurrentRow)> tables = [];
    private string? error;

    public void State(string label, object value, string? display = null) => facts.Add(new(ToKey(label), value));

    public void Detail(string label, object value) => facts.Add(new(ToKey(label), value));

    public void Message(string text)
    {
        // Text for people has no place in machine readable output.
    }

    public void Table(string key, string[] columns, IReadOnlyList<string[]> rows, TableOptions? options = null)
        => tables.Add((ToKey(key), columns, rows, options?.CurrentRow ?? -1));

    public void Fail(string text) => error = text;

    public void Flush()
    {
        using var buffer = new MemoryStream();
        using (var json = new Utf8JsonWriter(buffer))
        {
            json.WriteStartObject();

            foreach (var (key, value) in facts)
            {
                json.WritePropertyName(key);
                WriteValue(json, value);
            }

            foreach (var (key, columns, rows, currentRow) in tables)
            {
                json.WriteStartArray(key);
                for (var i = 0; i < rows.Count; i++)
                {
                    json.WriteStartObject();
                    for (var c = 0; c < columns.Length; c++)
                    {
                        json.WriteString(ToKey(columns[c]), rows[i][c]);
                    }

                    if (currentRow >= 0)
                    {
                        json.WriteBoolean("current", i == currentRow);
                    }

                    json.WriteEndObject();
                }

                json.WriteEndArray();
            }

            if (error is not null)
            {
                json.WriteString("error", error);
            }

            json.WriteEndObject();
        }

        stdout.WriteLine(Encoding.UTF8.GetString(buffer.ToArray()));
    }

    /// <summary>"Output device" becomes "output_device". Keys stay stable so scripts can rely on them.</summary>
    internal static string ToKey(string label) => label.Trim().ToLowerInvariant().Replace(' ', '_');

    private static void WriteValue(Utf8JsonWriter json, object value)
    {
        switch (value)
        {
            case bool b:
                json.WriteBooleanValue(b);
                break;
            case int i:
                json.WriteNumberValue(i);
                break;
            case long l:
                json.WriteNumberValue(l);
                break;
            case double d:
                json.WriteNumberValue(d);
                break;
            case string s:
                json.WriteStringValue(s);
                break;
            default:
                json.WriteStringValue(ConsoleOutput.FormatValue(value));
                break;
        }
    }
}

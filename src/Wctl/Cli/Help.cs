using System.Text;

namespace Wctl.Cli;

/// <summary>Renders the command table for people (terminal) and for the repo (COMMANDS.md).</summary>
public static class Help
{
    public const string ProductName = "Windows Terminal Control";

    public const string Tagline = "Fast commands for controlling Windows.";

    private const string UsageLine = "Usage: w <command> [arguments]    w <app> opens the app";

    public static int Overview(CommandTable table, IOutput output)
    {
        output.Message($"{ProductName} {VersionInfo.Current}");
        output.Detail("version", VersionInfo.Current);
        output.Message(Tagline);
        output.Message(string.Empty);
        output.Message(UsageLine);

        foreach (var group in table.Groups)
        {
            output.Message(string.Empty);
            var rows = group.Select(c => new[] { c.Name, string.Join(", ", c.Aliases), c.Summary }).ToList();
            output.Table(group.Key, ["command", "aliases", "description"], rows, new TableOptions(Title: group.Key, Header: false));
        }

        output.Message(string.Empty);
        output.Message("Run 'w help <command>' for details. Add --json to any command for machine readable output.");
        return ExitCodes.Ok;
    }

    public static int Command(CommandSpec command, IOutput output)
    {
        output.State("Command", command.Name);
        output.State("Usage", "w " + command.Usage);
        if (command.Aliases.Length > 0)
        {
            output.State("Aliases", string.Join(", ", command.Aliases));
        }

        output.State("Description", command.Summary);
        if (command.Details is not null)
        {
            output.Message(string.Empty);
            output.Message(command.Details);
        }

        return ExitCodes.Ok;
    }

    /// <summary>The content of COMMANDS.md. A test keeps the file in the repo equal to this.</summary>
    public static string Markdown(CommandTable table)
    {
        var sb = new StringBuilder();
        sb.Append("# ").Append(ProductName).Append(" commands\n\n");
        sb.Append("Generated from the command table in the code. Do not edit by hand. Regenerate with `scripts/update-commands.ps1`.\n\n");
        sb.Append("Usage: `w <command> [arguments]`. `w <app>` opens the app.\n");
        sb.Append("Every command has a long form and short aliases. On/off settings toggle when given no argument.\n");
        sb.Append("Add `--json` to any command for machine readable output.\n");
        sb.Append("Exit codes: 0 done, 1 the command could not do what was asked, 2 unexpected error (run again with `--debug`).\n");

        foreach (var group in table.Groups)
        {
            sb.Append("\n## ").Append(group.Key).Append("\n\n");
            sb.Append("| Command | Aliases | Description |\n");
            sb.Append("|---|---|---|\n");
            foreach (var command in group)
            {
                var usage = command.Usage.Replace("|", "\\|", StringComparison.Ordinal);
                var aliases = string.Join(", ", command.Aliases.Select(a => $"`{a}`"));
                sb.Append("| `w ").Append(usage).Append("` | ").Append(aliases).Append(" | ").Append(command.Summary).Append(" |\n");
            }
        }

        return sb.ToString();
    }
}

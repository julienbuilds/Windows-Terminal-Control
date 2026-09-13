using Spectre.Console.Testing;
using Wctl.Cli;
using Wctl.Commands;

namespace Wctl.Tests;

/// <param name="Stdout">What a person sees in the terminal.</param>
/// <param name="Stderr">Error text.</param>
/// <param name="RawStdout">Raw standard output: JSON or Markdown documents.</param>
internal sealed record RunResult(int ExitCode, string Stdout, string Stderr, string RawStdout);

/// <summary>Runs the whole tool in process, the same way Program.cs does, but against in-memory consoles.</summary>
internal static class TestHost
{
    public static RunResult Run(params string[] argv) => Run(Registry.Build(), argv);

    public static RunResult RunWith(CommandSpec extra, params string[] argv) => Run(TableWith(extra), argv);

    public static CommandTable TableWith(CommandSpec extra) => new(Registry.Build().All.Append(extra));

    public static RunResult Run(CommandTable table, params string[] argv)
    {
        var console = Console();
        var errorConsole = Console();
        using var raw = new StringWriter();

        var code = App.Run(argv, table, console, errorConsole, raw);

        return new RunResult(code, Normalize(console.Output), Normalize(errorConsole.Output), raw.ToString());
    }

    public static TestConsole Console()
    {
        var console = new TestConsole();
        console.Profile.Width = 200;
        return console;
    }

    public static string Normalize(string text) => text.Replace("\r\n", "\n", StringComparison.Ordinal);
}

using Spectre.Console.Testing;
using Wctl.Cli;
using Wctl.Commands;
using Wctl.Platform;

namespace Wctl.Tests;

/// <param name="Stdout">What a person sees in the terminal.</param>
/// <param name="Stderr">Error text.</param>
/// <param name="RawStdout">Raw standard output: JSON or Markdown documents.</param>
internal sealed record RunResult(int ExitCode, string Stdout, string Stderr, string RawStdout);

/// <summary>Fake versions of everything in <see cref="Services"/>, with the fakes exposed for assertions.</summary>
internal sealed class Fakes
{
    public FakeAudio Audio { get; } = new();

    public FakeWindows Windows { get; } = new();

    public FakeShell Shell { get; } = new();

    public FakeDisplay Display { get; } = new();

    public Services Services => new() { Audio = Audio, Windows = Windows, Shell = Shell, Display = Display };
}

/// <summary>Runs the whole tool in process, the same way Program.cs does, but against fakes and in-memory consoles.</summary>
internal static class TestHost
{
    public static RunResult Run(params string[] argv) => Run(new Fakes(), argv);

    public static RunResult Run(Fakes fakes, params string[] argv) => Run(Registry.Build(), fakes.Services, argv);

    public static RunResult RunWith(CommandSpec extra, params string[] argv) => Run(TableWith(extra), new Fakes().Services, argv);

    public static CommandTable TableWith(CommandSpec extra) => new(Registry.Build().All.Append(extra));

    public static RunResult Run(CommandTable table, Services services, params string[] argv)
    {
        var console = Console();
        var errorConsole = Console();
        using var raw = new StringWriter();

        var code = App.Run(argv, table, services, console, errorConsole, raw);

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

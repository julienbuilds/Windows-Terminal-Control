using Wctl.Platform;

namespace Wctl.Cli;

/// <summary>What a command gets when it runs.</summary>
/// <param name="Args">The words after the command name. "w vol +5" gives ["+5"].</param>
/// <param name="Options">Global flags.</param>
/// <param name="Output">Where results go.</param>
/// <param name="Stdout">Raw standard output, for commands that print a document (help --markdown).</param>
/// <param name="Table">All commands, for help.</param>
/// <param name="Services">Access to Windows: audio, windows, display, and so on. Fakes in tests.</param>
public sealed record Invocation(
    IReadOnlyList<string> Args,
    ParsedArgs Options,
    IOutput Output,
    TextWriter Stdout,
    CommandTable Table,
    Services Services)
{
    /// <summary>The first argument in lower case, or null when there is none.</summary>
    public string? FirstArg => Args.Count == 0 ? null : Args[0].ToLowerInvariant();
}

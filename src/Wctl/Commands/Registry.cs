using Wctl.Cli;

namespace Wctl.Commands;

/// <summary>Every command the tool offers, in help order.</summary>
public static class Registry
{
    public static CommandTable Build() => new(
    [
        HelpCommand.Spec,
        VersionCommand.Spec,
    ]);
}

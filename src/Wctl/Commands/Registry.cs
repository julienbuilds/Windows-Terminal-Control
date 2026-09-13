using Wctl.Cli;
using Wctl.Commands.Audio;

namespace Wctl.Commands;

/// <summary>Every command the tool offers, in help order.</summary>
public static class Registry
{
    public static CommandTable Build() => new(
    [
        VolCommand.Spec,
        MuteCommand.Spec,
        MicCommand.Spec,
        AudioCommand.Spec,
        HelpCommand.Spec,
        VersionCommand.Spec,
    ]);
}

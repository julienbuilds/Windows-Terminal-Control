using Wctl.Cli;
using Wctl.Commands.Apps;
using Wctl.Commands.Audio;
using Wctl.Commands.Windows;

namespace Wctl.Commands;

/// <summary>Every command the tool offers, in help order.</summary>
public static class Registry
{
    public static CommandTable Build() => new(
    [
        FocusCommand.Spec,
        CloseCommand.Spec,
        KillCommand.Spec,
        LsCommand.Spec,
        MoveCommand.Spec,
        CenterCommand.Spec,
        MaxCommand.Spec,
        MinCommand.Spec,
        TopCommand.Spec,
        MonitorsCommand.Spec,
        VolCommand.Spec,
        MuteCommand.Spec,
        MicCommand.Spec,
        AudioCommand.Spec,
        HelpCommand.Spec,
        VersionCommand.Spec,
    ]);
}

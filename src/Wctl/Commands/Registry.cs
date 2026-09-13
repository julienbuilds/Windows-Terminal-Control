using Wctl.Cli;
using Wctl.Commands.Apps;
using Wctl.Commands.Audio;
using Wctl.Commands.Files;
using Wctl.Commands.Windows;

namespace Wctl.Commands;

/// <summary>Every command the tool offers, in help order.</summary>
public static class Registry
{
    public static CommandTable Build() => new(
    [
        OpenCommand.Spec,
        FocusCommand.Spec,
        CloseCommand.Spec,
        KillCommand.Spec,
        AppsCommand.Spec,
        FolderCommand.Spec,
        RevealCommand.Spec,
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

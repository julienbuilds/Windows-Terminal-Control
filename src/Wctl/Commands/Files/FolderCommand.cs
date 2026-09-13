using Wctl.Cli;
using Wctl.Commands.Apps;
using Wctl.Platform;

namespace Wctl.Commands.Files;

public static class FolderCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "folder",
        Group = Groups.Files,
        Summary = "Open a folder in Explorer, the current one by default",
        Usage = "folder [<path>]",
        Details = @"'w folder' opens the current directory. Also 'w folder ..', 'w folder ~/Downloads' and 'w folder C:\dev'.",
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var shell = inv.Services.Shell;
        var input = inv.Args.Count == 0 ? "." : string.Join(' ', inv.Args);
        var fullPath = Paths.Resolve(input, shell.CurrentDirectory, shell.HomeDirectory);

        switch (shell.Classify(fullPath))
        {
            case PathKind.Missing:
                throw new WctlException($"There is no folder at '{fullPath}'.");
            case PathKind.File:
                throw new WctlException($"'{fullPath}' is a file, not a folder. 'w reveal' shows a file in Explorer.");
        }

        shell.Open(fullPath);
        inv.Output.State("Folder", fullPath);
        return ExitCodes.Ok;
    }
}

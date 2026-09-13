using Wctl.Cli;
using Wctl.Commands.Apps;
using Wctl.Platform;

namespace Wctl.Commands.Files;

public static class RevealCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "reveal",
        Aliases = ["select"],
        Group = Groups.Files,
        Summary = "Show a file in Explorer with the file selected",
        Usage = "reveal <file>",
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        if (inv.Args.Count == 0)
        {
            throw new WctlException($"Which file? Usage: w {Spec.Usage}");
        }

        var shell = inv.Services.Shell;
        var fullPath = Paths.Resolve(string.Join(' ', inv.Args), shell.CurrentDirectory, shell.HomeDirectory);
        if (shell.Classify(fullPath) == PathKind.Missing)
        {
            throw new WctlException($"There is no file or folder at '{fullPath}'.");
        }

        shell.Reveal(fullPath);
        inv.Output.State("Selected", fullPath);
        return ExitCodes.Ok;
    }
}

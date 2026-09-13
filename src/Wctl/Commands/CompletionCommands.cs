using Wctl.Cli;

namespace Wctl.Commands;

public static class CompleteCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "complete",
        Group = Groups.Help,
        Summary = "Print completion candidates for a half typed command line",
        Usage = "complete <word> ... [--end]",
        Details = "Used by tab completion, not meant to be typed. The last word is the one being completed; '--end' instead "
            + "means a fresh word is starting. Prints one candidate per line. Run 'w completion powershell' to set completion up.",
        Run = inv => Completion.Run(inv.Args, inv.Table, inv.Services, inv.Stdout),
    };
}

public static class CompletionCommand
{
    private const string Shell = "powershell";

    public static readonly CommandSpec Spec = new()
    {
        Name = "completion",
        Group = Groups.Help,
        Summary = "Print the tab completion script for your shell",
        Usage = "completion powershell",
        Details = "Add it to your PowerShell profile so completion is there in every terminal:\n"
            + "  w completion powershell >> $PROFILE\n"
            + "Then open a new terminal. Tab completes commands, scenes, apps, open windows, audio devices and monitors.",
        MaxArgs = 1,
        Complete = _ => [Shell],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var shell = inv.FirstArg ?? throw new WctlException($"Which shell? Usage: w {Spec.Usage}");
        if (shell != Shell)
        {
            throw new WctlException($"Only '{Shell}' is supported so far. Got '{inv.Args[0]}'.");
        }

        inv.Stdout.Write(Script);
        return ExitCodes.Ok;
    }

    /// <summary>
    /// Registers a native completer for "w". PowerShell gives the parsed command line, this hands the words to
    /// "w complete" and turns each line it prints back into a completion.
    /// </summary>
    internal const string Script = """

        # wctl tab completion
        Register-ArgumentCompleter -Native -CommandName w -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)

            $words = @()
            for ($i = 1; $i -lt $commandAst.CommandElements.Count; $i++) {
                $words += $commandAst.CommandElements[$i].ToString().Trim('"')
            }
            # PowerShell drops empty arguments, so a fresh word is announced with a marker instead.
            if ([string]::IsNullOrEmpty($wordToComplete)) { $words += '--end' }

            & w complete @words | ForEach-Object {
                $text = $_
                $insert = if ($text -match '\s') { '"' + $text + '"' } else { $text }
                [System.Management.Automation.CompletionResult]::new($insert, $text, 'ParameterValue', $text)
            }
        }

        """;
}

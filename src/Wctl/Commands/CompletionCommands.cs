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
            + "means a fresh word is starting. Prints one line per suggestion: the text, a tab, then its description. "
            + "Run 'w completion install $PROFILE' to set completion up.",
        Run = inv => Completion.Run(inv.Args, inv.Table, inv.Services, inv.Stdout),
    };
}

public static class CompletionCommand
{
    private const string Shell = "powershell";
    private const string Install = "install";

    public static readonly CommandSpec Spec = new()
    {
        Name = "completion",
        Group = Groups.Help,
        Summary = "Set up tab completion, or print the script for it",
        Usage = "completion install <file> | powershell",
        Details = "'w completion install $PROFILE' adds the completion script to your PowerShell profile and is safe to run "
            + "again; it replaces its own block rather than adding a second one, and it keeps the file's encoding so nothing "
            + "else in your profile breaks. Open a new terminal afterwards. 'w completion powershell' only prints the script, "
            + "for when you would rather place it yourself.",
        MaxArgs = 2,
        Complete = ctx => ctx.Position switch
        {
            0 => [new(Install, "add it to your profile"), new(Shell, "only print the script")],
            _ => [],
        },
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        switch (inv.FirstArg)
        {
            case Shell:
                inv.Stdout.Write(Script);
                return ExitCodes.Ok;

            case Install:
                return InstallInto(inv);

            case null:
                throw new WctlException($"What should it do? Usage: w {Spec.Usage}");

            default:
                throw new WctlException($"Expected '{Install}' or '{Shell}'. Got '{inv.Args[0]}'.");
        }
    }

    private static int InstallInto(Invocation inv)
    {
        if (inv.Args.Count < 2)
        {
            throw new WctlException($"Which file? Usually your PowerShell profile: w completion {Install} $PROFILE");
        }

        var path = Path.GetFullPath(inv.Args[1], inv.Services.Shell.CurrentDirectory);
        var existing = File.Exists(path) ? File.ReadAllBytes(path) : [];
        var (bytes, what) = ProfileBlock.Merge(existing, Script);

        var folder = Path.GetDirectoryName(path);
        if (folder is { Length: > 0 })
        {
            Directory.CreateDirectory(folder);
        }

        File.WriteAllBytes(path, bytes);

        inv.Output.State("Profile", path);
        inv.Output.State("Completion", what switch
        {
            ProfileBlock.Result.Created => "installed, the file was created",
            ProfileBlock.Result.Replaced => "updated",
            _ => "installed",
        });
        inv.Output.Message("Open a new terminal, then press Ctrl+Space after a command to see the suggestions.");
        return ExitCodes.Ok;
    }

    /// <summary>
    /// Registers a native completer for "w". PowerShell gives the parsed command line, this hands the words to
    /// "w complete" and turns each line it prints back into a completion with its description.
    /// </summary>
    internal const string Script = """
        # Windows Terminal Control tab completion
        Register-ArgumentCompleter -Native -CommandName w -ScriptBlock {
            param($wordToComplete, $commandAst, $cursorPosition)

            $words = @()
            for ($i = 1; $i -lt $commandAst.CommandElements.Count; $i++) {
                $words += $commandAst.CommandElements[$i].ToString().Trim('"')
            }
            # PowerShell drops empty arguments, so a fresh word is announced with a marker instead.
            if ([string]::IsNullOrEmpty($wordToComplete)) { $words += '--end' }

            & w complete @words | ForEach-Object {
                # Each line is the suggestion, a tab, then the description shown beside it in the menu.
                $parts = $_ -split "`t", 2
                $text = $parts[0]
                $tip = if ($parts.Count -gt 1 -and $parts[1]) { $parts[1] } else { $text }
                $insert = if ($text -match '\s') { '"' + $text + '"' } else { $text }
                [System.Management.Automation.CompletionResult]::new($insert, $text, 'ParameterValue', $tip)
            }
        }

        # Descriptions appear in the completion menu, which is Ctrl+Space by default.
        # Uncomment the next line to get that menu from Tab as well, instead of cycling one by one.
        # Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete
        """;
}

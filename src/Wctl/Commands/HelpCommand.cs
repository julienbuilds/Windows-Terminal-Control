using Wctl.Cli;

namespace Wctl.Commands;

public static class HelpCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "help",
        Group = Groups.Help,
        Summary = "Show all commands, or details for one",
        Usage = "help [<command>]",
        Details = "'w help --markdown' prints the command list as Markdown. scripts/update-commands.ps1 uses it to generate COMMANDS.md.",
        MaxArgs = 1,
        Complete = ctx => ctx.Position == 0 ? ctx.Table.All.Select(c => c.Name) : [],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        if (inv.Options.Markdown)
        {
            inv.Stdout.Write(Help.Markdown(inv.Table));
            return ExitCodes.Ok;
        }

        if (inv.Args.Count == 0)
        {
            return Help.Overview(inv.Table, inv.Output);
        }

        var command = inv.Table.Find(inv.Args[0])
            ?? throw new WctlException($"Unknown command '{inv.Args[0]}'. Run 'w help' to see all commands.");
        return Help.Command(command, inv.Output);
    }
}

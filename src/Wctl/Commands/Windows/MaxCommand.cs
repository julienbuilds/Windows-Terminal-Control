using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Windows;

public static class MaxCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "max",
        Aliases = ["maximize"],
        Group = Groups.Windows,
        Summary = "Maximize a window",
        Usage = "max <window> [off]",
        Details = "'w max firefox off' restores the normal size.",
        MaxArgs = 2,
        Complete = ShowCommand.Complete,
        Run = inv => ShowCommand.Run(inv, "max <window> [off]", WindowState.Maximized, "maximized"),
    };
}

public static class MinCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "min",
        Aliases = ["minimize"],
        Group = Groups.Windows,
        Summary = "Minimize a window",
        Usage = "min <window> [off]",
        Details = "'w min firefox off' brings it back.",
        MaxArgs = 2,
        Complete = ShowCommand.Complete,
        Run = inv => ShowCommand.Run(inv, "min <window> [off]", WindowState.Minimized, "minimized"),
    };
}

internal static class ShowCommand
{
    public static IEnumerable<string> Complete(CompletionContext ctx)
        => ctx.Position == 0 ? ctx.WindowTargets() : ctx.Position == 1 ? ["off"] : [];

    public static int Run(Invocation inv, string usage, WindowState state, string word)
    {
        var match = Target.Require(inv, usage);
        var off = inv.Args.Count > 1;
        if (off && inv.Args[1] is not "off")
        {
            throw new WctlException($"Expected 'off' or nothing after the window. Got '{inv.Args[1]}'. Usage: w {usage}");
        }

        inv.Services.Windows.Show(match.First.Handle, off ? WindowState.Restored : state);
        inv.Output.Action(match.Label, off ? "restored" : word);
        return ExitCodes.Ok;
    }
}

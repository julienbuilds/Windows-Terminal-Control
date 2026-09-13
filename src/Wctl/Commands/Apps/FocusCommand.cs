using Wctl.Cli;
using Wctl.Commands.Windows;

namespace Wctl.Commands.Apps;

public static class FocusCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "focus",
        Aliases = ["f"],
        Group = Groups.Apps,
        Summary = "Bring an app's window to the front",
        Usage = "focus <window>",
        Details = "A minimized window is restored. With several windows of one app, the most recent one comes to the front.",
        MaxArgs = 1,
        Complete = ctx => ctx.Position == 0 ? ctx.WindowTargets() : [],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var match = Target.Require(inv, Spec.Usage);
        if (!inv.Services.Windows.Focus(match.First.Handle))
        {
            throw new WctlException($"Windows did not let '{match.Label}' come to the front. Click any window once and try again.");
        }

        inv.Output.Action(match.Label, "focused");
        return ExitCodes.Ok;
    }
}

using Wctl.Cli;

namespace Wctl.Commands.Windows;

public static class TopCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "top",
        Group = Groups.Windows,
        Summary = "Keep a window always on top",
        Usage = "top <window> [on | off]",
        Details = "Without on or off it toggles.",
        MaxArgs = 2,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var match = Target.Require(inv, Spec.Usage);
        var window = match.First;
        var on = inv.Args.Count > 1
            ? inv.Args[1] switch
            {
                "on" => true,
                "off" => false,
                _ => throw new WctlException($"Expected on, off or nothing. Got '{inv.Args[1]}'."),
            }
            : !window.TopMost;

        inv.Services.Windows.SetTopMost(window.Handle, on);
        inv.Output.Action(match.Label, on ? "always on top" : "no longer on top");
        return ExitCodes.Ok;
    }
}

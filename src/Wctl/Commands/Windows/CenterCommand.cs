using Wctl.Cli;

namespace Wctl.Commands.Windows;

public static class CenterCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "center",
        Group = Groups.Windows,
        Summary = "Center a window on its monitor",
        Usage = "center <window>",
        Details = "Keeps the size. A maximized or minimized window is restored first.",
        MaxArgs = 1,
        Complete = ctx => ctx.Position == 0 ? ctx.WindowTargets() : [],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var match = Target.Require(inv, Spec.Usage);
        var window = match.First;
        var windows = inv.Services.Windows;
        var monitor = Target.MonitorOf(windows.Monitors(), window);

        Target.Normalize(windows, window);
        var frame = windows.GetFrame(window.Handle);
        windows.SetFrame(window.Handle, Geometry.Center(monitor.WorkArea, frame.Width, frame.Height));

        inv.Output.Action(match.Label, $"centered on monitor {monitor.Index}");
        return ExitCodes.Ok;
    }
}

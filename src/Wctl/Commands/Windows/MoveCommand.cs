using System.Globalization;
using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Windows;

public static class MoveCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "move",
        Aliases = ["m", "snap"],
        Group = Groups.Windows,
        Summary = "Snap a window to a side, move it to a monitor, or place it exactly",
        Usage = "move <window> left | right | <monitor> | <x> <y> <width> <height>",
        Details = "'w move firefox left' fills the left half of its monitor. 'w move firefox 2' moves it to monitor 2 "
            + "(see 'w monitors'). Four numbers place the visible frame at x,y with that size, in screen pixels.",
        MaxArgs = 5,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var match = Target.Require(inv, Spec.Usage);
        var window = match.First;
        var windows = inv.Services.Windows;
        var monitors = windows.Monitors();
        var current = Target.MonitorOf(monitors, window);
        var rest = inv.Args.Skip(1).ToList();

        if (rest.Count == 1 && rest[0] is "left" or "l" or "right" or "r")
        {
            var left = rest[0] is "left" or "l";
            Target.Normalize(windows, window);
            windows.SetFrame(window.Handle, left ? Geometry.LeftHalf(current.WorkArea) : Geometry.RightHalf(current.WorkArea));
            inv.Output.Action(match.Label, $"{(left ? "left" : "right")} half of monitor {current.Index}");
            return ExitCodes.Ok;
        }

        if (rest.Count == 1 && TryNumber(rest[0], out var monitorNumber)
            || rest.Count == 2 && rest[0] is "monitor" or "mon" && TryNumber(rest[1], out monitorNumber))
        {
            var target = monitors.FirstOrDefault(m => m.Index == monitorNumber)
                ?? throw new WctlException($"There is no monitor {monitorNumber}. Run 'w monitors' to see them.");

            var wasMaximized = window.Maximized;
            Target.Normalize(windows, window);
            var frame = windows.GetFrame(window.Handle);
            windows.SetFrame(window.Handle, Geometry.MoveToWorkArea(frame, current.WorkArea, target.WorkArea));
            if (wasMaximized)
            {
                windows.Show(window.Handle, WindowState.Maximized);
            }

            inv.Output.Action(match.Label, $"monitor {target.Index}");
            return ExitCodes.Ok;
        }

        if (rest.Count == 4 && TryNumber(rest[0], out var x) && TryNumber(rest[1], out var y)
            && TryNumber(rest[2], out var width) && TryNumber(rest[3], out var height))
        {
            if (width <= 0 || height <= 0)
            {
                throw new WctlException("Width and height must be greater than zero.");
            }

            Target.Normalize(windows, window);
            var frame = Rect.FromSize(x, y, width, height);
            windows.SetFrame(window.Handle, frame);
            inv.Output.Action(match.Label, frame.ToString());
            return ExitCodes.Ok;
        }

        throw new WctlException($"Where to? Usage: w {Spec.Usage}");
    }

    private static bool TryNumber(string text, out int value)
        => int.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
}

using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Windows;

/// <summary>Shared steps for commands that act on one window.</summary>
internal static class Target
{
    /// <summary>Resolves the first argument to windows, or fails with the command's usage.</summary>
    public static WindowMatch Require(Invocation inv, string usage)
    {
        if (inv.Args.Count == 0)
        {
            throw new WctlException($"Which window? Usage: w {usage}");
        }

        return WindowTarget.Resolve(inv.Services.Windows.List(), inv.Args[0]);
    }

    /// <summary>The monitor the window is on. Falls back to the primary monitor when Windows did not say.</summary>
    public static MonitorInfo MonitorOf(IReadOnlyList<MonitorInfo> monitors, WindowInfo window)
    {
        if (monitors.Count == 0)
        {
            throw new WctlException("No monitors found.");
        }

        return monitors.FirstOrDefault(m => m.Index == window.Monitor)
            ?? monitors.FirstOrDefault(m => m.Primary)
            ?? monitors[0];
    }

    /// <summary>Brings a minimized or maximized window back to normal before it is moved.</summary>
    public static void Normalize(IWindows windows, WindowInfo window)
    {
        if (window.Minimized || window.Maximized)
        {
            windows.Show(window.Handle, WindowState.Restored);
        }
    }
}

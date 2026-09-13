using System.Globalization;
using Wctl.Cli;

namespace Wctl.Commands.Windows;

public static class MonitorsCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "monitors",
        Group = Groups.Windows,
        Summary = "List monitors with their numbers",
        Usage = "monitors",
        Details = "The number is what 'w move <window> <monitor>' expects. The marked row is the primary monitor.",
        MaxArgs = 0,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var monitors = inv.Services.Windows.Monitors();
        if (monitors.Count == 0)
        {
            throw new WctlException("No monitors found.");
        }

        var rows = monitors.Select(m => new[]
        {
            m.Index.ToString(CultureInfo.InvariantCulture),
            $"{m.Bounds.Width}x{m.Bounds.Height}",
            $"{m.Bounds.Left},{m.Bounds.Top}",
            m.Device,
        }).ToList();

        var primary = monitors.ToList().FindIndex(m => m.Primary);
        inv.Output.Table("monitors", ["index", "resolution", "position", "device"], rows, new TableOptions(CurrentRow: primary));
        return ExitCodes.Ok;
    }
}

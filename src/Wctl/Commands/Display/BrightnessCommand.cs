using System.Globalization;
using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Display;

public static class BrightnessCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "brightness",
        Aliases = ["bright"],
        Group = Groups.Display,
        Summary = "Show or set monitor brightness",
        Usage = "brightness [<n> | +<n> | -<n>] [monitor <m>]",
        Details = "Talks to the monitor over DDC/CI, the same channel its own menu uses. Not every monitor supports it, "
            + "and some need DDC/CI switched on in their menu. Some monitors drop a request now and then; run the command again. "
            + "Without 'monitor <m>' it acts on every monitor that answers. Laptop panels are not covered yet.",
        MaxArgs = 3,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var (valueArg, monitorNumber) = ParseArgs(inv.Args);
        var display = inv.Services.Display;
        var known = inv.Services.Windows.Monitors();
        var selected = Select(display.GetBrightness(), known, monitorNumber);

        var errors = new List<string>();
        if (valueArg is not null)
        {
            Level.Apply(valueArg, 0, "Brightness"); // Validates the text once, before any monitor is touched.
            foreach (var monitor in selected)
            {
                try
                {
                    display.SetBrightness(monitor.Device, Level.Apply(valueArg, monitor.Percent, "Brightness"));
                }
                catch (WctlException e)
                {
                    errors.Add(e.Message);
                }
            }

            var wanted = selected.Select(m => m.Device).ToHashSet();
            selected = display.GetBrightness().Where(m => wanted.Contains(m.Device)).ToList();
        }

        Report(selected, known, inv.Output);
        if (errors.Count > 0)
        {
            throw new WctlException(string.Join(" ", errors));
        }

        return ExitCodes.Ok;
    }

    private static (string? Value, int? Monitor) ParseArgs(IReadOnlyList<string> args)
    {
        var words = args.ToList();
        int? monitor = null;

        var at = words.FindIndex(w => w is "monitor" or "mon");
        if (at >= 0)
        {
            if (at + 1 >= words.Count || !int.TryParse(words[at + 1], NumberStyles.None, CultureInfo.InvariantCulture, out var number))
            {
                throw new WctlException($"Which monitor? Usage: w {Spec.Usage}");
            }

            monitor = number;
            words.RemoveRange(at, 2);
        }

        if (words.Count > 1)
        {
            throw new WctlException($"Too many arguments. Usage: w {Spec.Usage}");
        }

        var value = words.Count == 1 && words[0] != "status" ? words[0] : null;
        return (value, monitor);
    }

    private static List<BrightnessMonitor> Select(IReadOnlyList<BrightnessMonitor> all, IReadOnlyList<MonitorInfo> known, int? monitorNumber)
    {
        if (monitorNumber is int number)
        {
            var device = known.FirstOrDefault(m => m.Index == number)?.Device
                ?? throw new WctlException($"There is no monitor {number}. Run 'w monitors' to see them.");
            var match = all.FirstOrDefault(m => m.Device == device);
            if (match is null || !match.Supported)
            {
                throw new WctlException($"Monitor {number} does not answer brightness requests over DDC/CI. Check that DDC/CI is enabled in its menu.");
            }

            return [match];
        }

        var supported = all.Where(m => m.Supported).ToList();
        if (supported.Count == 0)
        {
            throw new WctlException("No monitor answers brightness requests over DDC/CI. Check that DDC/CI is enabled in the monitor's menu.");
        }

        return supported;
    }

    private static void Report(List<BrightnessMonitor> monitors, IReadOnlyList<MonitorInfo> known, IOutput output)
    {
        if (monitors.Count == 1)
        {
            output.State("Brightness", monitors[0].Percent, $"{monitors[0].Percent}%");
            return;
        }

        var rows = monitors
            .Select(m => new[] { MonitorNames.Label(known, m.Device), $"{m.Percent}%" })
            .ToList();
        output.Table("monitors", ["monitor", "brightness"], rows);
    }
}

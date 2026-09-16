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
            + "Acts on every monitor that answers unless 'monitor <m>' picks one: a number from 'w monitors', or 'main' for "
            + "the primary monitor. Laptop panels are not covered yet.",
        MaxArgs = 3,
        Complete = ctx => ctx.WithMonitorOption(new Candidate("status", "only show the brightness")),
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var known = inv.Services.Windows.Monitors();
        var words = inv.Args.ToList();
        var monitor = MonitorNames.Take(words, known, Spec.Usage);
        if (words.Count > 1)
        {
            throw new WctlException($"Too many arguments. Usage: w {Spec.Usage}");
        }

        var valueArg = words.Count == 1 && words[0] != "status" ? words[0] : null;
        var display = inv.Services.Display;
        var selected = Select(display.GetBrightness(), monitor);

        var errors = new List<string>();
        if (valueArg is not null)
        {
            Level.Apply(valueArg, 0, "Brightness"); // Validates the text once, before any monitor is touched.
            foreach (var m in selected)
            {
                try
                {
                    display.SetBrightness(m.Device, Level.Apply(valueArg, m.Percent, "Brightness"));
                }
                catch (WctlException e)
                {
                    errors.Add(e.Message);
                }
            }

            var devices = selected.Select(m => m.Device).ToHashSet();
            selected = display.GetBrightness().Where(m => devices.Contains(m.Device)).ToList();
        }

        Report(selected, known, inv.Output);
        if (errors.Count > 0)
        {
            throw new WctlException(string.Join(" ", errors));
        }

        return ExitCodes.Ok;
    }

    private static List<BrightnessMonitor> Select(IReadOnlyList<BrightnessMonitor> all, MonitorInfo? monitor)
    {
        if (monitor is not null)
        {
            var match = all.FirstOrDefault(m => m.Device == monitor.Device);
            if (match is null || !match.Supported)
            {
                throw new WctlException($"Monitor {monitor.Index} does not answer brightness requests over DDC/CI. Check that DDC/CI is enabled in its menu.");
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

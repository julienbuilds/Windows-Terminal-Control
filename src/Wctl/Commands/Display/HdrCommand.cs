using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Display;

public static class HdrCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "hdr",
        Aliases = ["h"],
        Group = Groups.Display,
        Summary = "Turn HDR on or off",
        Usage = "hdr [on | off | status] [monitor <m>]",
        Details = "Without an argument it toggles. Acts on every display that supports HDR unless 'monitor <m>' picks one: "
            + "a number from 'w monitors', or 'main' for the primary monitor. When displays disagree, the toggle turns all of "
            + "them on first. 'w hdr status' only shows the state.",
        MaxArgs = 3,
        Complete = ctx => ctx.WithMonitorOption(
            new("on", "turn HDR on"),
            new("off", "turn HDR off"),
            new("status", "only show the state")),
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var monitors = inv.Services.Windows.Monitors();
        var words = inv.Args.ToList();
        var monitor = MonitorNames.Take(words, monitors, Spec.Usage);
        if (words.Count > 1)
        {
            throw new WctlException($"Too many arguments. Usage: w {Spec.Usage}");
        }

        var word = words.Count == 1 ? words[0].ToLowerInvariant() : null;
        var display = inv.Services.Display;
        var selected = Select(display.GetHdr(), monitor);

        bool? target = word switch
        {
            null => !selected.All(d => d.Enabled),
            "on" => true,
            "off" => false,
            "status" => null,
            _ => throw new WctlException($"Expected on, off, status or nothing. Got '{words[0]}'."),
        };

        if (target is bool wanted)
        {
            foreach (var d in selected.Where(d => d.Enabled != wanted))
            {
                display.SetHdr(d.Device, wanted);
            }

            var devices = selected.Select(d => d.Device).ToHashSet();
            selected = display.GetHdr().Where(d => devices.Contains(d.Device)).ToList();
        }

        Report(selected, monitors, inv.Output);
        return ExitCodes.Ok;
    }

    private static List<HdrDisplay> Select(IReadOnlyList<HdrDisplay> all, MonitorInfo? monitor)
    {
        if (monitor is not null)
        {
            var match = all.FirstOrDefault(d => d.Device == monitor.Device);
            if (match is null || !match.Supported)
            {
                throw new WctlException($"Monitor {monitor.Index} does not support HDR.");
            }

            return [match];
        }

        var capable = all.Where(d => d.Supported).ToList();
        if (capable.Count == 0)
        {
            throw new WctlException("No display supports HDR.");
        }

        return capable;
    }

    private static void Report(List<HdrDisplay> displays, IReadOnlyList<MonitorInfo> monitors, IOutput output)
    {
        var on = displays.Count(d => d.Enabled);
        if (on == displays.Count)
        {
            output.State("HDR", true);
        }
        else if (on == 0)
        {
            output.State("HDR", false);
        }
        else
        {
            output.State("HDR", "mixed");
        }

        if (displays.Count > 1)
        {
            var rows = displays
                .Select(d => new[] { MonitorNames.Label(monitors, d.Device), d.Enabled ? "on" : "off" })
                .ToList();
            output.Table("displays", ["monitor", "hdr"], rows);
        }
    }
}

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
        Usage = "hdr [on | off | status]",
        Details = "Without an argument it toggles. Acts on every display that supports HDR. "
            + "When displays disagree, the toggle turns all of them on first. 'w hdr status' only shows the state.",
        MaxArgs = 1,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var display = inv.Services.Display;
        var all = display.GetHdr();
        var capable = all.Where(d => d.Supported).ToList();
        if (capable.Count == 0)
        {
            throw new WctlException("No display supports HDR.");
        }

        bool? target = inv.FirstArg switch
        {
            null => !capable.All(d => d.Enabled),
            "on" => true,
            "off" => false,
            "status" => null,
            _ => throw new WctlException($"Expected on, off, status or nothing. Got '{inv.Args[0]}'."),
        };

        if (target is bool wanted)
        {
            foreach (var d in capable.Where(d => d.Enabled != wanted))
            {
                display.SetHdr(d.Device, wanted);
            }

            capable = display.GetHdr().Where(d => d.Supported).ToList();
        }

        Report(capable, inv.Services.Windows.Monitors(), inv.Output);
        return ExitCodes.Ok;
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

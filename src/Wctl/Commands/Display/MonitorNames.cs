using Wctl.Platform;

namespace Wctl.Commands.Display;

/// <summary>Turns a GDI device name into the monitor number people see in 'w monitors'.</summary>
internal static class MonitorNames
{
    public static string Label(IReadOnlyList<MonitorInfo> monitors, string device)
    {
        var monitor = monitors.FirstOrDefault(m => m.Device == device);
        return monitor is null ? device : monitor.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

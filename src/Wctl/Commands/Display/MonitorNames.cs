using System.Globalization;
using Wctl.Platform;

namespace Wctl.Commands.Display;

/// <summary>The 'monitor &lt;m&gt;' option shared by display commands, and monitor numbers for output.</summary>
internal static class MonitorNames
{
    /// <summary>Turns a GDI device name into the monitor number people see in 'w monitors'.</summary>
    public static string Label(IReadOnlyList<MonitorInfo> monitors, string device)
    {
        var monitor = monitors.FirstOrDefault(m => m.Device == device);
        return monitor is null ? device : monitor.Index.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Removes "monitor &lt;m&gt;" (or "mon &lt;m&gt;") from the words and returns that monitor.
    /// &lt;m&gt; is a number from 'w monitors', or "main" for the primary monitor. Null when the option is absent.
    /// </summary>
    public static MonitorInfo? Take(List<string> words, IReadOnlyList<MonitorInfo> monitors, string usage)
    {
        var at = words.FindIndex(w => w is "monitor" or "mon");
        if (at < 0)
        {
            return null;
        }

        if (at + 1 >= words.Count)
        {
            throw new WctlException($"Which monitor? Usage: w {usage}");
        }

        var word = words[at + 1].ToLowerInvariant();
        words.RemoveRange(at, 2);

        if (word is "main" or "primary")
        {
            return monitors.FirstOrDefault(m => m.Primary) ?? throw new WctlException("No primary monitor found.");
        }

        if (!int.TryParse(word, NumberStyles.None, CultureInfo.InvariantCulture, out var number))
        {
            throw new WctlException($"Which monitor? Expected a number from 'w monitors' or 'main'. Got '{word}'.");
        }

        return monitors.FirstOrDefault(m => m.Index == number)
            ?? throw new WctlException($"There is no monitor {number}. Run 'w monitors' to see them.");
    }
}

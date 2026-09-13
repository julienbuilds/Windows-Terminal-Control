namespace Wctl.Platform;

/// <summary>HDR and brightness per display. Displays are named by their GDI device, for example \\.\DISPLAY1.</summary>
public interface IDisplay
{
    /// <summary>HDR support and state of every active display.</summary>
    IReadOnlyList<HdrDisplay> GetHdr();

    void SetHdr(string device, bool enabled);

    /// <summary>Brightness of every monitor that answers over DDC/CI. Others are listed as unsupported.</summary>
    IReadOnlyList<BrightnessMonitor> GetBrightness();

    void SetBrightness(string device, int percent);
}

/// <param name="Device">GDI device name, matches <see cref="MonitorInfo.Device"/>.</param>
public sealed record HdrDisplay(string Device, bool Supported, bool Enabled);

/// <param name="Device">GDI device name, matches <see cref="MonitorInfo.Device"/>.</param>
/// <param name="Percent">0 to 100, mapped from the monitor's own range.</param>
public sealed record BrightnessMonitor(string Device, bool Supported, int Percent);

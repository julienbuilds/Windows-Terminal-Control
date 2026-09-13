using Wctl.Platform;

namespace Wctl.Tests;

/// <summary>Two displays: the first supports HDR (off), the second does not. Both answer brightness requests.</summary>
internal sealed class FakeDisplay : IDisplay
{
    public const string Display1 = @"\\.\DISPLAY1";
    public const string Display2 = @"\\.\DISPLAY2";

    public List<HdrDisplay> Hdr { get; } =
    [
        new(Display1, Supported: true, Enabled: false),
        new(Display2, Supported: false, Enabled: false),
    ];

    public List<BrightnessMonitor> Brightness { get; } =
    [
        new(Display1, Supported: true, 60),
        new(Display2, Supported: true, 40),
    ];

    public List<(string Device, bool Enabled)> HdrChanges { get; } = [];

    public List<(string Device, int Percent)> BrightnessChanges { get; } = [];

    /// <summary>Devices whose brightness set fails, like a monitor that drops DDC/CI requests.</summary>
    public HashSet<string> FailingDevices { get; } = [];

    public IReadOnlyList<HdrDisplay> GetHdr() => Hdr;

    public void SetHdr(string device, bool enabled)
    {
        HdrChanges.Add((device, enabled));
        var index = Hdr.FindIndex(d => d.Device == device);
        if (index < 0)
        {
            throw new InvalidOperationException($"Unknown display '{device}'.");
        }

        Hdr[index] = Hdr[index] with { Enabled = enabled };
    }

    public IReadOnlyList<BrightnessMonitor> GetBrightness() => Brightness;

    public void SetBrightness(string device, int percent)
    {
        if (FailingDevices.Contains(device))
        {
            throw new WctlException($"{device} does not answer brightness requests over DDC/CI.");
        }

        BrightnessChanges.Add((device, percent));
        var index = Brightness.FindIndex(m => m.Device == device);
        if (index < 0)
        {
            throw new InvalidOperationException($"Unknown monitor '{device}'.");
        }

        Brightness[index] = Brightness[index] with { Percent = percent };
    }
}

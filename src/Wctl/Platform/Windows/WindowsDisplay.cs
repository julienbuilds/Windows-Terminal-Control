using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Wctl.Platform.Windows;

/// <summary>HDR through the Display Configuration API, brightness through DDC/CI.</summary>
public sealed unsafe class WindowsDisplay : IDisplay
{
    [ThreadStatic]
    private static List<nint>? collected;

    public IReadOnlyList<HdrDisplay> GetHdr()
    {
        var result = new List<HdrDisplay>();
        foreach (var path in QueryPaths())
        {
            var info = new DisplayConfigGetAdvancedColorInfo
            {
                Header = Header(DisplayConfig.TypeGetAdvancedColorInfo, sizeof(DisplayConfigGetAdvancedColorInfo), path.TargetInfo),
            };

            var status = DisplayConfig.GetAdvancedColorInfo(ref info);
            if (status != 0)
            {
                throw new WctlException($"Windows refused to read the HDR state (error {status}).");
            }

            result.Add(new HdrDisplay(
                SourceName(path),
                (info.Value & DisplayConfig.AdvancedColorSupported) != 0,
                (info.Value & DisplayConfig.AdvancedColorEnabled) != 0));
        }

        return result;
    }

    public void SetHdr(string device, bool enabled)
    {
        foreach (var path in QueryPaths())
        {
            if (SourceName(path) != device)
            {
                continue;
            }

            var state = new DisplayConfigSetAdvancedColorState
            {
                Header = Header(DisplayConfig.TypeSetAdvancedColorState, sizeof(DisplayConfigSetAdvancedColorState), path.TargetInfo),
                Value = enabled ? 1u : 0u,
            };

            var status = DisplayConfig.SetAdvancedColorState(ref state);
            if (status != 0)
            {
                throw new WctlException($"Windows refused to change HDR on {device} (error {status}).");
            }

            return;
        }

        throw new WctlException($"There is no active display '{device}'.");
    }

    public IReadOnlyList<BrightnessMonitor> GetBrightness()
    {
        var result = new List<BrightnessMonitor>();
        foreach (var (handle, device) in Monitors())
        {
            var physical = PhysicalMonitors(handle);
            try
            {
                if (physical.Length == 0 || !TryReadBrightness(physical[0].Handle, out var min, out var current, out var max))
                {
                    result.Add(new BrightnessMonitor(device, Supported: false, 0));
                    continue;
                }

                result.Add(new BrightnessMonitor(device, Supported: true, ToPercent(min, current, max)));
            }
            finally
            {
                Destroy(physical);
            }
        }

        return result;
    }

    public void SetBrightness(string device, int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Brightness must be between 0 and 100.");
        }

        foreach (var (handle, name) in Monitors())
        {
            if (name != device)
            {
                continue;
            }

            var physical = PhysicalMonitors(handle);
            try
            {
                if (physical.Length == 0 || !TryReadBrightness(physical[0].Handle, out var min, out _, out var max))
                {
                    throw new WctlException($"{device} does not answer brightness requests over DDC/CI.");
                }

                var value = min + (uint)Math.Round((max - min) * percent / 100.0);
                uint actual = 0;
                for (var attempt = 0; attempt < Attempts; attempt++)
                {
                    if (!TrySetBrightness(physical[0].Handle, value))
                    {
                        throw new WctlException($"{device} refused the brightness change (error {Marshal.GetLastPInvokeError()}).");
                    }

                    // Some monitors say yes and do nothing, mostly when writes come quickly after each other.
                    // Read back and write again until the monitor really shows the value.
                    Thread.Sleep(RetryDelay * 3);
                    if (!TryReadBrightness(physical[0].Handle, out _, out actual, out _) || Math.Abs((long)actual - value) <= 1)
                    {
                        return;
                    }
                }

                throw new WctlException($"{device} accepted {percent}% but still reports {ToPercent(min, actual, max)}%. The monitor did not apply the change.");
            }
            finally
            {
                Destroy(physical);
            }
        }

        throw new WctlException($"There is no monitor '{device}'.");
    }

    // DDC/CI runs over a slow I2C link and drops requests now and then. A few retries make it dependable.
    private const int Attempts = 4;
    private const int RetryDelay = 80;

    private static bool TryReadBrightness(nint physicalMonitor, out uint minimum, out uint current, out uint maximum)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (Dxva2.GetMonitorBrightness(physicalMonitor, out minimum, out current, out maximum))
            {
                return true;
            }

            Thread.Sleep(RetryDelay << attempt);
        }

        minimum = current = maximum = 0;
        return false;
    }

    private static bool TrySetBrightness(nint physicalMonitor, uint value)
    {
        for (var attempt = 0; attempt < Attempts; attempt++)
        {
            if (Dxva2.SetMonitorBrightness(physicalMonitor, value))
            {
                return true;
            }

            Thread.Sleep(RetryDelay << attempt);
        }

        return false;
    }

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
    private static int CollectMonitor(nint monitor, nint hdc, RECT* rect, nint lParam)
    {
        collected!.Add(monitor);
        return 1;
    }

    private static DisplayConfigDeviceInfoHeader Header(uint type, int size, in DisplayConfigPathTargetInfo target)
        => new() { Type = type, Size = (uint)size, AdapterId = target.AdapterId, Id = target.Id };

    private static DisplayConfigPathInfo[] QueryPaths()
    {
        var status = DisplayConfig.GetDisplayConfigBufferSizes(DisplayConfig.QdcOnlyActivePaths, out var pathCount, out var modeCount);
        if (status != 0)
        {
            throw new WctlException($"Windows refused to list displays (error {status}).");
        }

        var paths = new DisplayConfigPathInfo[pathCount];
        var modes = new DisplayConfigModeInfo[modeCount];
        status = DisplayConfig.QueryDisplayConfig(DisplayConfig.QdcOnlyActivePaths, ref pathCount, paths, ref modeCount, modes, 0);
        if (status != 0)
        {
            throw new WctlException($"Windows refused to describe the displays (error {status}).");
        }

        return paths[..(int)pathCount];
    }

    private static string SourceName(in DisplayConfigPathInfo path)
    {
        var name = new DisplayConfigSourceDeviceName
        {
            Header = new DisplayConfigDeviceInfoHeader
            {
                Type = DisplayConfig.TypeGetSourceName,
                Size = (uint)sizeof(DisplayConfigSourceDeviceName),
                AdapterId = path.SourceInfo.AdapterId,
                Id = path.SourceInfo.Id,
            },
        };

        var status = DisplayConfig.GetSourceName(ref name);
        if (status != 0)
        {
            throw new WctlException($"Windows refused to name a display (error {status}).");
        }

        return new string(name.ViewGdiDeviceName);
    }

    private static List<(nint Handle, string Device)> Monitors()
    {
        var handles = new List<nint>(4);
        collected = handles;
        try
        {
            if (!User32.EnumDisplayMonitors(0, 0, &CollectMonitor, 0))
            {
                throw new WctlException($"Windows refused to list monitors (error {Marshal.GetLastPInvokeError()}).");
            }
        }
        finally
        {
            collected = null;
        }

        var result = new List<(nint, string)>(handles.Count);
        foreach (var handle in handles)
        {
            var info = new MONITORINFOEX { Size = (uint)sizeof(MONITORINFOEX) };
            if (!User32.GetMonitorInfoW(handle, ref info))
            {
                throw new WctlException($"Could not read monitor information (error {Marshal.GetLastPInvokeError()}).");
            }

            result.Add((handle, new string(info.Device)));
        }

        return result;
    }

    private static PhysicalMonitor[] PhysicalMonitors(nint monitor)
    {
        if (!Dxva2.GetNumberOfPhysicalMonitorsFromHMONITOR(monitor, out var count) || count == 0)
        {
            return [];
        }

        var monitors = new PhysicalMonitor[count];
        return Dxva2.GetPhysicalMonitorsFromHMONITOR(monitor, count, monitors) ? monitors : [];
    }

    private static void Destroy(PhysicalMonitor[] monitors)
    {
        if (monitors.Length > 0)
        {
            Dxva2.DestroyPhysicalMonitors((uint)monitors.Length, monitors);
        }
    }

    private static int ToPercent(uint min, uint current, uint max)
        => max <= min ? 0 : (int)Math.Round((current - min) * 100.0 / (max - min));
}

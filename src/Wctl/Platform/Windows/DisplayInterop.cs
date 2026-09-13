using System.Runtime.InteropServices;

namespace Wctl.Platform.Windows;

// Display Configuration API (HDR) and DDC/CI (brightness). Declared by hand so they work in Native AOT.
// Struct layouts follow wingdi.h and physicalmonitorenumerationapi.h.

[StructLayout(LayoutKind.Sequential)]
internal struct Luid
{
    public int LowPart;
    public int HighPart;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathSourceInfo
{
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIdx;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigRational
{
    public uint Numerator;
    public uint Denominator;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathTargetInfo
{
    public Luid AdapterId;
    public uint Id;
    public uint ModeInfoIdx;
    public uint OutputTechnology;
    public uint Rotation;
    public uint Scaling;
    public DisplayConfigRational RefreshRate;
    public uint ScanLineOrdering;
    public int TargetAvailable;
    public uint StatusFlags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigPathInfo
{
    public DisplayConfigPathSourceInfo SourceInfo;
    public DisplayConfigPathTargetInfo TargetInfo;
    public uint Flags;
}

/// <summary>64 bytes. The union at the end is never read here, only sized correctly.</summary>
[StructLayout(LayoutKind.Sequential)]
internal unsafe struct DisplayConfigModeInfo
{
    public uint InfoType;
    public uint Id;
    public Luid AdapterId;
    public fixed byte Union[48];
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigDeviceInfoHeader
{
    public uint Type;
    public uint Size;
    public Luid AdapterId;
    public uint Id;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct DisplayConfigSourceDeviceName
{
    public DisplayConfigDeviceInfoHeader Header;
    public fixed char ViewGdiDeviceName[32];
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigGetAdvancedColorInfo
{
    public DisplayConfigDeviceInfoHeader Header;

    /// <summary>Bit 0: supported. Bit 1: enabled. Bit 2: wide color enforced. Bit 3: force disabled.</summary>
    public uint Value;
    public uint ColorEncoding;
    public uint BitsPerColorChannel;
}

[StructLayout(LayoutKind.Sequential)]
internal struct DisplayConfigSetAdvancedColorState
{
    public DisplayConfigDeviceInfoHeader Header;

    /// <summary>Bit 0: enable advanced color.</summary>
    public uint Value;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal unsafe struct PhysicalMonitor
{
    public nint Handle;
    public fixed char Description[128];
}

internal static partial class DisplayConfig
{
    public const uint QdcOnlyActivePaths = 0x00000002;
    public const uint TypeGetSourceName = 1;
    public const uint TypeGetAdvancedColorInfo = 9;
    public const uint TypeSetAdvancedColorState = 10;
    public const uint AdvancedColorSupported = 0x1;
    public const uint AdvancedColorEnabled = 0x2;

    [LibraryImport("user32.dll")]
    public static partial int GetDisplayConfigBufferSizes(uint flags, out uint pathCount, out uint modeCount);

    [LibraryImport("user32.dll")]
    public static partial int QueryDisplayConfig(
        uint flags,
        ref uint pathCount,
        [Out] DisplayConfigPathInfo[] paths,
        ref uint modeCount,
        [Out] DisplayConfigModeInfo[] modes,
        nint currentTopology);

    [LibraryImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    public static partial int GetSourceName(ref DisplayConfigSourceDeviceName info);

    [LibraryImport("user32.dll", EntryPoint = "DisplayConfigGetDeviceInfo")]
    public static partial int GetAdvancedColorInfo(ref DisplayConfigGetAdvancedColorInfo info);

    [LibraryImport("user32.dll", EntryPoint = "DisplayConfigSetDeviceInfo")]
    public static partial int SetAdvancedColorState(ref DisplayConfigSetAdvancedColorState info);
}

internal static partial class Dxva2
{
    [LibraryImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetNumberOfPhysicalMonitorsFromHMONITOR(nint monitor, out uint count);

    [LibraryImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetPhysicalMonitorsFromHMONITOR(nint monitor, uint count, [Out] PhysicalMonitor[] monitors);

    [LibraryImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorBrightness(nint physicalMonitor, out uint minimum, out uint current, out uint maximum);

    [LibraryImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetMonitorBrightness(nint physicalMonitor, uint value);

    [LibraryImport("dxva2.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyPhysicalMonitors(uint count, PhysicalMonitor[] monitors);
}

using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace Wctl.Platform.Windows;

// Core Audio COM interfaces, declared by hand with source generated COM so they work in Native AOT.
// Method order must match the vtable order in the Windows SDK headers (mmdeviceapi.h, endpointvolume.h).

internal enum EDataFlow
{
    Render = 0,
    Capture = 1,
}

internal enum ERole
{
    Console = 0,
    Multimedia = 1,
    Communications = 2,
}

[StructLayout(LayoutKind.Sequential)]
internal struct PropertyKey(Guid formatId, uint propertyId)
{
    public Guid FormatId = formatId;
    public uint PropertyId = propertyId;
}

/// <summary>PROPVARIANT, 24 bytes on x64. Only the LPWSTR case is read here.</summary>
[StructLayout(LayoutKind.Sequential)]
internal struct PropVariant
{
    public const ushort VtLpwstr = 31;

    public ushort VarType;
    public ushort Reserved1;
    public ushort Reserved2;
    public ushort Reserved3;
    public nint Pointer;
    public nint Padding;
}

[GeneratedComInterface]
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
internal partial interface IMMDeviceEnumerator
{
    IMMDeviceCollection EnumAudioEndpoints(EDataFlow dataFlow, uint stateMask);

    IMMDevice GetDefaultAudioEndpoint(EDataFlow dataFlow, ERole role);

    IMMDevice GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id);

    void RegisterEndpointNotificationCallback(nint client);

    void UnregisterEndpointNotificationCallback(nint client);
}

[GeneratedComInterface]
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
internal partial interface IMMDeviceCollection
{
    uint GetCount();

    IMMDevice Item(uint index);
}

[GeneratedComInterface]
[Guid("D666063F-1587-4E43-81F1-B948E807363F")]
internal partial interface IMMDevice
{
    /// <returns>A raw interface pointer for <paramref name="iid"/>. Wrap it with <see cref="Com.Wrap{T}"/>.</returns>
    nint Activate(in Guid iid, uint clsCtx, nint activationParams);

    IPropertyStore OpenPropertyStore(uint stgmAccess);

    [return: MarshalAs(UnmanagedType.LPWStr)]
    string GetId();

    uint GetState();
}

[GeneratedComInterface]
[Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99")]
internal partial interface IPropertyStore
{
    uint GetCount();

    PropertyKey GetAt(uint index);

    PropVariant GetValue(in PropertyKey key);

    void SetValue(in PropertyKey key, in PropVariant value);

    void Commit();
}

[GeneratedComInterface]
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A")]
internal partial interface IAudioEndpointVolume
{
    void RegisterControlChangeNotify(nint notify);

    void UnregisterControlChangeNotify(nint notify);

    uint GetChannelCount();

    void SetMasterVolumeLevel(float levelDb, nint eventContext);

    void SetMasterVolumeLevelScalar(float level, nint eventContext);

    float GetMasterVolumeLevel();

    float GetMasterVolumeLevelScalar();

    void SetChannelVolumeLevel(uint channel, float levelDb, nint eventContext);

    void SetChannelVolumeLevelScalar(uint channel, float level, nint eventContext);

    float GetChannelVolumeLevel(uint channel);

    float GetChannelVolumeLevelScalar(uint channel);

    void SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, nint eventContext);

    [return: MarshalAs(UnmanagedType.Bool)]
    bool GetMute();

    void GetVolumeStepInfo(out uint step, out uint stepCount);

    void VolumeStepUp(nint eventContext);

    void VolumeStepDown(nint eventContext);

    uint QueryHardwareSupport();

    void GetVolumeRange(out float minDb, out float maxDb, out float incrementDb);
}

/// <summary>
/// Undocumented interface Windows itself uses to change the default device. Every audio switcher tool relies on it.
/// Only SetDefaultEndpoint is called. The other methods are declared to keep the vtable slots in order.
/// </summary>
[GeneratedComInterface]
[Guid("F8679F50-850A-41CF-9C72-430F290290C8")]
internal partial interface IPolicyConfig
{
    [PreserveSig]
    int GetMixFormat(nint deviceId, nint format);

    [PreserveSig]
    int GetDeviceFormat(nint deviceId, int isDefault, nint format);

    [PreserveSig]
    int ResetDeviceFormat(nint deviceId);

    [PreserveSig]
    int SetDeviceFormat(nint deviceId, nint endpointFormat, nint mixFormat);

    [PreserveSig]
    int GetProcessingPeriod(nint deviceId, int isDefault, nint defaultPeriod, nint minimumPeriod);

    [PreserveSig]
    int SetProcessingPeriod(nint deviceId, nint period);

    [PreserveSig]
    int GetShareMode(nint deviceId, nint mode);

    [PreserveSig]
    int SetShareMode(nint deviceId, nint mode);

    [PreserveSig]
    int GetPropertyValue(nint deviceId, int fxStore, nint key, nint value);

    [PreserveSig]
    int SetPropertyValue(nint deviceId, int fxStore, nint key, nint value);

    void SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ERole role);

    [PreserveSig]
    int SetEndpointVisibility(nint deviceId, int visible);
}

internal static partial class Ole32
{
    public const uint ClsctxAll = 0x17;
    public const uint CoinitApartmentThreaded = 0x2;
    public const int SFalse = 1;
    public const int RpcEChangedMode = unchecked((int)0x80010106);

    [LibraryImport("ole32.dll")]
    public static partial int CoInitializeEx(nint reserved, uint coInit);

    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(in Guid clsid, nint outer, uint context, in Guid iid, out nint instance);

    [LibraryImport("ole32.dll")]
    public static partial int PropVariantClear(ref PropVariant value);
}

/// <summary>Creating and wrapping COM objects in an AOT friendly way.</summary>
internal static class Com
{
    private static readonly StrategyBasedComWrappers Wrappers = new();
    private static bool initialized;

    public static T Create<T>(Guid clsid, Guid iid) where T : class
    {
        Initialize();
        var hr = Ole32.CoCreateInstance(in clsid, 0, Ole32.ClsctxAll, in iid, out var pointer);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        return Wrap<T>(pointer);
    }

    /// <summary>Takes ownership of one reference on <paramref name="pointer"/>.</summary>
    public static T Wrap<T>(nint pointer) where T : class
    {
        try
        {
            return (T)Wrappers.GetOrCreateObjectForComInstance(pointer, CreateObjectFlags.None);
        }
        finally
        {
            Marshal.Release(pointer);
        }
    }

    private static void Initialize()
    {
        if (initialized)
        {
            return;
        }

        var hr = Ole32.CoInitializeEx(0, Ole32.CoinitApartmentThreaded);
        if (hr < 0 && hr != Ole32.RpcEChangedMode)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        initialized = true;
    }
}

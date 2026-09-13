using System.Runtime.InteropServices;

namespace Wctl.Platform.Windows;

/// <summary>Speaker and microphone control through the Windows Core Audio API.</summary>
public sealed class CoreAudio : IAudio
{
    private const uint DeviceStateActive = 0x1;
    private const uint StgmRead = 0x0;
    private const int ENotFound = unchecked((int)0x80070490);

    private static readonly Guid ClsidMMDeviceEnumerator = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid IidMMDeviceEnumerator = new("A95664D2-9614-4F35-A746-DE8DB63617E6");
    private static readonly Guid IidAudioEndpointVolume = new("5CDF2C82-841E-4546-9722-0CF74078229A");
    private static readonly Guid ClsidPolicyConfigClient = new("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9");
    private static readonly Guid IidPolicyConfig = new("F8679F50-850A-41CF-9C72-430F290290C8");
    private static readonly PropertyKey PkeyDeviceFriendlyName = new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);

    private IMMDeviceEnumerator? enumerator;

    private IMMDeviceEnumerator Enumerator => enumerator ??= Com.Create<IMMDeviceEnumerator>(ClsidMMDeviceEnumerator, IidMMDeviceEnumerator);

    public int GetVolume() => ToPercent(DefaultVolume(EDataFlow.Render).GetMasterVolumeLevelScalar());

    public void SetVolume(int percent)
    {
        if (percent is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(percent), percent, "Volume must be between 0 and 100.");
        }

        DefaultVolume(EDataFlow.Render).SetMasterVolumeLevelScalar(percent / 100f, 0);
    }

    public bool GetMuted() => DefaultVolume(EDataFlow.Render).GetMute();

    public void SetMuted(bool muted) => DefaultVolume(EDataFlow.Render).SetMute(muted, 0);

    public bool GetMicMuted() => DefaultVolume(EDataFlow.Capture).GetMute();

    public void SetMicMuted(bool muted) => DefaultVolume(EDataFlow.Capture).SetMute(muted, 0);

    public IReadOnlyList<AudioDevice> GetOutputDevices()
    {
        var collection = Enumerator.EnumAudioEndpoints(EDataFlow.Render, DeviceStateActive);
        var count = collection.GetCount();
        var devices = new List<AudioDevice>((int)count);
        for (uint i = 0; i < count; i++)
        {
            var device = collection.Item(i);
            var id = device.GetId();
            devices.Add(new AudioDevice(id, FriendlyName(device) ?? id));
        }

        return devices;
    }

    public string? GetDefaultOutputId()
    {
        try
        {
            return Enumerator.GetDefaultAudioEndpoint(EDataFlow.Render, ERole.Console).GetId();
        }
        catch (COMException e) when (e.HResult == ENotFound)
        {
            return null;
        }
    }

    public void SetDefaultOutput(string deviceId)
    {
        var policy = Com.Create<IPolicyConfig>(ClsidPolicyConfigClient, IidPolicyConfig);
        policy.SetDefaultEndpoint(deviceId, ERole.Console);
        policy.SetDefaultEndpoint(deviceId, ERole.Multimedia);
        policy.SetDefaultEndpoint(deviceId, ERole.Communications);
    }

    private static int ToPercent(float scalar) => (int)MathF.Round(Math.Clamp(scalar, 0f, 1f) * 100f);

    private static string? FriendlyName(IMMDevice device)
    {
        var store = device.OpenPropertyStore(StgmRead);
        var value = store.GetValue(in PkeyDeviceFriendlyName);
        try
        {
            return value.VarType == PropVariant.VtLpwstr ? Marshal.PtrToStringUni(value.Pointer) : null;
        }
        finally
        {
            Ole32.PropVariantClear(ref value);
        }
    }

    private IAudioEndpointVolume DefaultVolume(EDataFlow flow)
    {
        IMMDevice device;
        try
        {
            device = Enumerator.GetDefaultAudioEndpoint(flow, ERole.Console);
        }
        catch (COMException e) when (e.HResult == ENotFound)
        {
            throw new WctlException(flow == EDataFlow.Render
                ? "No audio output device found."
                : "No microphone found.");
        }

        var pointer = device.Activate(in IidAudioEndpointVolume, Ole32.ClsctxAll, 0);
        return Com.Wrap<IAudioEndpointVolume>(pointer);
    }
}

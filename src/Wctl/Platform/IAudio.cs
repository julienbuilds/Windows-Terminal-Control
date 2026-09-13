namespace Wctl.Platform;

/// <summary>Speaker volume, mute, microphone mute, and the default output device.</summary>
public interface IAudio
{
    /// <summary>Speaker volume in percent, 0 to 100.</summary>
    int GetVolume();

    void SetVolume(int percent);

    bool GetMuted();

    void SetMuted(bool muted);

    /// <summary>Mute state of the default microphone.</summary>
    bool GetMicMuted();

    void SetMicMuted(bool muted);

    /// <summary>All active output devices.</summary>
    IReadOnlyList<AudioDevice> GetOutputDevices();

    /// <summary>Id of the default output device, or null when there is none.</summary>
    string? GetDefaultOutputId();

    /// <summary>Makes the device the default output for everything: media, system sounds and calls.</summary>
    void SetDefaultOutput(string deviceId);
}

/// <param name="Id">Windows endpoint id. Stable across reboots.</param>
/// <param name="Name">What Windows shows, for example "Speakers (FiiO K11)".</param>
public sealed record AudioDevice(string Id, string Name);

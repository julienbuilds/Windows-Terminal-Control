using Wctl.Platform;

namespace Wctl.Tests;

internal sealed class FakeAudio : IAudio
{
    public int Volume { get; set; } = 47;

    public bool Muted { get; set; }

    public bool MicMuted { get; set; }

    public List<AudioDevice> Outputs { get; } =
    [
        new("id-fiio", "Speakers (FiiO K11)"),
        new("id-focusrite", "Speakers (Focusrite USB Audio)"),
        new("id-btd", "Headphones (BTD 600)"),
    ];

    public string? DefaultOutputId { get; set; } = "id-fiio";

    public int GetVolume() => Volume;

    public void SetVolume(int percent) => Volume = percent;

    public bool GetMuted() => Muted;

    public void SetMuted(bool muted) => Muted = muted;

    public bool GetMicMuted() => MicMuted;

    public void SetMicMuted(bool muted) => MicMuted = muted;

    public IReadOnlyList<AudioDevice> GetOutputDevices() => Outputs;

    public string? GetDefaultOutputId() => DefaultOutputId;

    public void SetDefaultOutput(string deviceId)
    {
        if (!Outputs.Exists(d => d.Id == deviceId))
        {
            throw new InvalidOperationException($"Unknown device id '{deviceId}'.");
        }

        DefaultOutputId = deviceId;
    }
}

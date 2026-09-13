using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Audio;

/// <summary>The one way volume and mute state are printed, so every audio command looks the same.</summary>
internal static class AudioReport
{
    public static void Volume(IAudio audio, IOutput output)
    {
        var volume = audio.GetVolume();
        var muted = audio.GetMuted();
        output.State("Volume", volume, muted ? $"{volume}% (muted)" : $"{volume}%");
        output.Detail("Muted", muted);
    }

    public static void Mic(IAudio audio, IOutput output)
        => output.State("Mic", audio.GetMicMuted() ? "muted" : "unmuted");
}

using Wctl.Platform;

namespace Wctl.Tests;

internal sealed class FakeMedia : IMedia
{
    /// <summary>The keys that were sent, in order.</summary>
    public List<string> Sent { get; } = [];

    public void PlayPause() => Sent.Add("play/pause");

    public void NextTrack() => Sent.Add("next");

    public void PreviousTrack() => Sent.Add("previous");

    public void StopPlayback() => Sent.Add("stop");
}

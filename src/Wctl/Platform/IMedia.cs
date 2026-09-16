namespace Wctl.Platform;

/// <summary>
/// Transport control for whatever is playing: music, a video, a podcast.
/// There is nothing to read back here. Windows sends these to the app that currently owns media, and does not
/// report what happened, so every one of these is a request rather than a change with a result.
/// </summary>
public interface IMedia
{
    /// <summary>Pauses what is playing, or resumes what is paused. One key does both.</summary>
    void PlayPause();

    void NextTrack();

    void PreviousTrack();

    void StopPlayback();
}

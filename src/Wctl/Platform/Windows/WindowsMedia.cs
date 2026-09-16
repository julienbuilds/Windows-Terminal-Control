namespace Wctl.Platform.Windows;

/// <summary>
/// Sends the media keys a keyboard would send. Windows delivers them to the app that currently owns media,
/// which is the same routing the buttons on a keyboard get, so this works with every player that listens for them.
/// </summary>
public sealed class WindowsMedia : IMedia
{
    public void PlayPause() => Press(User32.VkMediaPlayPause);

    public void NextTrack() => Press(User32.VkMediaNextTrack);

    public void PreviousTrack() => Press(User32.VkMediaPrevTrack);

    public void StopPlayback() => Press(User32.VkMediaStop);

    private static void Press(byte key)
    {
        // Media keys are extended keys, so the flag has to be set for apps that check the scan code.
        User32.keybd_event(key, 0, User32.KeyEventFExtendedKey, 0);
        User32.keybd_event(key, 0, User32.KeyEventFExtendedKey | User32.KeyEventFKeyUp, 0);
    }
}

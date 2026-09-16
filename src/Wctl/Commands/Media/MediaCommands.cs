using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Media;

public static class PlayCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "play",
        Aliases = ["pause", "pp"],
        Group = Groups.Media,
        Summary = "Pause what is playing, or resume it",
        Usage = "play",
        Details = "One key does both, exactly like the play button on a keyboard, so 'w play' and 'w pause' are the same "
            + "command and it toggles. " + MediaAction.Caveat,
        MaxArgs = 0,
        Run = inv => MediaAction.Run(inv, m => m.PlayPause(), "play or pause"),
    };
}

public static class NextCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "next",
        Aliases = ["skip"],
        Group = Groups.Media,
        Summary = "Skip to the next track",
        Usage = "next",
        Details = MediaAction.Caveat,
        MaxArgs = 0,
        Run = inv => MediaAction.Run(inv, m => m.NextTrack(), "next track"),
    };
}

public static class PrevCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "prev",
        Aliases = ["previous", "back"],
        Group = Groups.Media,
        Summary = "Go back to the previous track",
        Usage = "prev",
        Details = "Most players jump to the start of the current track first, and only go back a track when you run it twice. "
            + MediaAction.Caveat,
        MaxArgs = 0,
        Run = inv => MediaAction.Run(inv, m => m.PreviousTrack(), "previous track"),
    };
}

public static class StopCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "stop",
        Group = Groups.Media,
        Summary = "Stop playback",
        Usage = "stop",
        Details = "Not every player supports stop. Those that do not usually treat it as a pause. " + MediaAction.Caveat,
        MaxArgs = 0,
        Run = inv => MediaAction.Run(inv, m => m.StopPlayback(), "stop"),
    };
}

internal static class MediaAction
{
    public const string Caveat =
        "This sends the same key a keyboard media button sends, so it reaches whichever app currently owns media. "
        + "Windows does not report back, so the command can only say what it sent, not what happened. "
        + "If nothing is playing, nothing happens.";

    public static int Run(Invocation inv, Action<IMedia> send, string what)
    {
        send(inv.Services.Media);
        inv.Output.State("Media", what);
        return ExitCodes.Ok;
    }
}

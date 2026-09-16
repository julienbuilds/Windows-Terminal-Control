using Wctl.Cli;

namespace Wctl.Commands.Audio;

public static class VolCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "vol",
        Aliases = ["volume", "v"],
        Group = Groups.Audio,
        Summary = "Show or set the speaker volume",
        Usage = "vol [<n> | +<n> | -<n> | mute | unmute]",
        Details = "Setting a volume also unmutes, like the volume keys do. 'w vol mute' mutes, 'w mute' toggles.",
        MaxArgs = 1,
        Complete = _ => [new("mute", "silence the speakers"), new("unmute", "sound back on"), new("status", "only show the volume")],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var audio = inv.Services.Audio;
        switch (inv.FirstArg)
        {
            case null or "status":
                break;
            case "mute":
                audio.SetMuted(true);
                break;
            case "unmute":
                audio.SetMuted(false);
                break;
            default:
                audio.SetVolume(Level.Apply(inv.Args[0], audio.GetVolume(), "Volume"));
                audio.SetMuted(false);
                break;
        }

        AudioReport.Volume(audio, inv.Output);
        return ExitCodes.Ok;
    }
}

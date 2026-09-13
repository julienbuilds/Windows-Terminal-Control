using Wctl.Cli;

namespace Wctl.Commands.Audio;

public static class MuteCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "mute",
        Group = Groups.Audio,
        Summary = "Mute or unmute the speakers",
        Usage = "mute [on | off]",
        Details = "Without an argument it toggles. 'w mute status' only shows the state.",
        MaxArgs = 1,
        Complete = _ => ["on", "off", "status"],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var audio = inv.Services.Audio;
        switch (inv.FirstArg)
        {
            case null:
                audio.SetMuted(!audio.GetMuted());
                break;
            case "on":
                audio.SetMuted(true);
                break;
            case "off":
                audio.SetMuted(false);
                break;
            case "status":
                break;
            default:
                throw new WctlException($"Expected on, off or nothing. Got '{inv.Args[0]}'.");
        }

        AudioReport.Volume(audio, inv.Output);
        return ExitCodes.Ok;
    }
}

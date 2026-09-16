using Wctl.Cli;

namespace Wctl.Commands.Audio;

public static class MicCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "mic",
        Group = Groups.Audio,
        Summary = "Mute or unmute the microphone",
        Usage = "mic [mute | unmute]",
        Details = "Without an argument it toggles. 'w mic status' only shows the state. "
            + "Acts on the default microphone from Windows Settings > Sound > Input.",
        MaxArgs = 1,
        Complete = _ => [new("mute", "nobody hears you"), new("unmute", "they hear you again"), new("status", "only show the state")],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var audio = inv.Services.Audio;
        switch (inv.FirstArg)
        {
            case null:
                audio.SetMicMuted(!audio.GetMicMuted());
                break;
            case "mute" or "on":
                audio.SetMicMuted(true);
                break;
            case "unmute" or "off":
                audio.SetMicMuted(false);
                break;
            case "status":
                break;
            default:
                throw new WctlException($"Expected mute, unmute or nothing. Got '{inv.Args[0]}'.");
        }

        AudioReport.Mic(audio, inv.Output);
        return ExitCodes.Ok;
    }
}

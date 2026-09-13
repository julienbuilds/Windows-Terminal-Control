using Wctl.Cli;

namespace Wctl.Commands.Power;

public static class WaitCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "wait",
        Group = Groups.System,
        Summary = "Wait for a while",
        Usage = "wait <duration>",
        Details = "Mostly for scenes: 'wait 2s' between opening an app and moving its window. Durations: 45s, 2m, 1h30m, or a plain number of minutes.",
        MaxArgs = 1,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        if (inv.Args.Count == 0)
        {
            throw new WctlException($"How long? Usage: w {Spec.Usage}");
        }

        var duration = Duration.Parse(inv.Args[0]);
        inv.Output.Status($"Waiting {Duration.Format(duration)}");
        inv.Services.Clock.Delay(duration, CancellationToken.None);
        inv.Output.Status(string.Empty);
        inv.Output.State("Waited", Duration.Format(duration));
        return ExitCodes.Ok;
    }
}

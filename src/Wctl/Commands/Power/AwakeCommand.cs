using Wctl.Cli;

namespace Wctl.Commands.Power;

public static class AwakeCommand
{
    private static readonly TimeSpan Tick = TimeSpan.FromSeconds(1);

    public static readonly CommandSpec Spec = new()
    {
        Name = "awake",
        Group = Groups.System,
        Summary = "Keep the PC and screen awake for a while",
        Usage = "awake [<duration>]",
        Details = "'w awake 90m' shows a countdown and keeps the PC from sleeping and the screen from turning off until it ends. "
            + "Ctrl+C stops early. Without a duration it runs until Ctrl+C. Durations: 45s, 90m, 1h30m, or a plain number of minutes.",
        MaxArgs = 1,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var duration = inv.Args.Count == 0 ? (TimeSpan?)null : Duration.Parse(inv.Args[0]);
        var clock = inv.Services.Clock;
        var output = inv.Output;

        using var cancel = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) =>
        {
            e.Cancel = true; // Let the loop end normally so the awake request is released and the summary is printed.
            cancel.Cancel();
        };
        Console.CancelKeyPress += onCancel;

        var start = clock.Now;
        var end = duration is null ? (DateTimeOffset?)null : start + duration.Value;
        var stopped = false;

        try
        {
            using var hold = inv.Services.Power.KeepAwake();
            while (true)
            {
                var now = clock.Now;
                if (end is not null && now >= end)
                {
                    break;
                }

                var remaining = end - now;
                output.Status(remaining is null
                    ? "Awake until you press Ctrl+C"
                    : $"Awake for another {Duration.Format(remaining.Value)}. Ctrl+C stops early.");

                var step = remaining is null || remaining.Value > Tick ? Tick : remaining.Value;
                if (!clock.Delay(step, cancel.Token))
                {
                    stopped = true;
                    break;
                }
            }
        }
        finally
        {
            Console.CancelKeyPress -= onCancel;
        }

        output.Status(string.Empty);
        var kept = Duration.Format(clock.Now - start);
        output.State("Kept awake", stopped && end is not null ? $"{kept} (stopped early)" : kept);
        return ExitCodes.Ok;
    }
}

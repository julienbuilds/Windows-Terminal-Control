using Wctl.Cli;
using Wctl.Commands.Windows;

namespace Wctl.Commands.Apps;

public static class KillCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "kill",
        Group = Groups.Apps,
        Summary = "Force an app to quit",
        Usage = "kill <app>",
        Details = "Terminates the process without asking. Unsaved work is lost. Also works for apps without a window, by process name.",
        MaxArgs = 1,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        if (inv.Args.Count == 0)
        {
            throw new WctlException($"Which app? Usage: w {Spec.Usage}");
        }

        var windows = inv.Services.Windows;
        var query = inv.Args[0];
        var match = WindowTarget.Find(windows.List(), query);

        if (match is null)
        {
            var name = query.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? query[..^4] : query;
            var count = windows.KillByName(name);
            if (count == 0)
            {
                throw new WctlException($"Nothing named '{query}' is running.");
            }

            inv.Output.Action(name, Killed(count));
            return ExitCodes.Ok;
        }

        var processIds = match.Windows.Select(w => w.ProcessId).Distinct().ToList();
        foreach (var processId in processIds)
        {
            windows.Kill(processId);
        }

        inv.Output.Action(match.Label, Killed(processIds.Count));
        return ExitCodes.Ok;
    }

    private static string Killed(int count) => count == 1 ? "killed" : $"killed {count} processes";
}

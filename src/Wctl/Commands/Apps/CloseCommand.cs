using Wctl.Cli;
using Wctl.Commands.Windows;

namespace Wctl.Commands.Apps;

public static class CloseCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "close",
        Aliases = ["x"],
        Group = Groups.Apps,
        Summary = "Close an app, like clicking its X",
        Usage = "close <window>",
        Details = "An app name closes all its windows. A title or a number from 'w ls' closes that one window. "
            + "The app may still ask to save. Use 'w kill' to force it.",
        MaxArgs = 1,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var match = Target.Require(inv, Spec.Usage);
        foreach (var window in match.Windows)
        {
            inv.Services.Windows.Close(window.Handle);
        }

        inv.Output.Action(match.Label, match.Windows.Count == 1 ? "closing" : $"closing {match.Windows.Count} windows");
        return ExitCodes.Ok;
    }
}

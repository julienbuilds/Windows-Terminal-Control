using Wctl.Cli;

namespace Wctl.Commands;

public static class VersionCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "version",
        Group = Groups.Help,
        Summary = "Show the wctl version",
        Usage = "version",
        Run = inv =>
        {
            inv.Output.State("wctl", VersionInfo.Current);
            return ExitCodes.Ok;
        },
    };
}

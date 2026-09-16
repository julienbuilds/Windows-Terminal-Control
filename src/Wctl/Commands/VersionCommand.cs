using Wctl.Cli;

namespace Wctl.Commands;

public static class VersionCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "version",
        Group = Groups.Help,
        Summary = "Show the version",
        Usage = "version",
        MaxArgs = 0,
        Run = inv =>
        {
            inv.Output.State("w", VersionInfo.Current);
            return ExitCodes.Ok;
        },
    };
}

using Wctl.Cli;

namespace Wctl.Commands.Power;

public static class LockCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "lock",
        Group = Groups.System,
        Summary = "Lock the screen",
        Usage = "lock",
        Details = "Same as Win+L.",
        MaxArgs = 0,
        Run = inv =>
        {
            inv.Services.Power.Lock();
            inv.Output.Action("PC", "locked");
            return ExitCodes.Ok;
        },
    };
}

public static class SleepCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "sleep",
        Group = Groups.System,
        Summary = "Put the PC to sleep",
        Usage = "sleep",
        Details = "Goes to sleep right away, no confirmation. Unsaved work stays in memory like with the power menu.",
        MaxArgs = 0,
        Run = inv =>
        {
            inv.Output.Action("PC", "sleeping");
            inv.Services.Power.Sleep();
            return ExitCodes.Ok;
        },
    };
}

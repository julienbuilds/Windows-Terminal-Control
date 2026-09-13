using System.Globalization;
using Wctl.Cli;

namespace Wctl.Commands.Windows;

public static class LsCommand
{
    private const int TitleWidth = 60;

    public static readonly CommandSpec Spec = new()
    {
        Name = "ls",
        Aliases = ["windows"],
        Group = Groups.Windows,
        Summary = "List open windows with their numbers",
        Usage = "ls",
        Details = "The number works as a target everywhere: 'w move 2 left', 'w close 4'. The marked row is the active window.",
        MaxArgs = 0,
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var windows = inv.Services.Windows.List();
        if (windows.Count == 0)
        {
            inv.Output.Message("No windows open.");
        }

        var rows = windows.Select((w, i) => new[]
        {
            (i + 1).ToString(CultureInfo.InvariantCulture),
            w.App,
            Shorten(w.Title),
            w.Monitor == 0 ? "?" : w.Monitor.ToString(CultureInfo.InvariantCulture),
        }).ToList();

        var active = windows.ToList().FindIndex(w => w.Active);
        inv.Output.Table("windows", ["index", "app", "title", "monitor"], rows, new TableOptions(CurrentRow: active));
        return ExitCodes.Ok;
    }

    private static string Shorten(string title) => title.Length <= TitleWidth ? title : title[..(TitleWidth - 1)] + "…";
}

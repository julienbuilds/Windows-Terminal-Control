using Wctl.Cli;

namespace Wctl.Commands.Apps;

public static class AppsCommand
{
    private const string RefreshWord = "--refresh";

    public static readonly CommandSpec Spec = new()
    {
        Name = "apps",
        Group = Groups.Apps,
        Summary = "List installed apps, optionally filtered",
        Usage = "apps [<filter>] [--refresh]",
        Details = "Shows the names 'w open' understands. The exe column is the short name for desktop apps: 'w open code'. "
            + "The list is kept for a day so opening an app stays instant; '--refresh' rebuilds it now. "
            + "An app that was just installed is also found on the next lookup, which refreshes by itself.",
        Complete = _ => [RefreshWord],
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var words = inv.Args.ToList();
        var refresh = words.RemoveAll(w => w.Equals(RefreshWord, StringComparison.OrdinalIgnoreCase)) > 0;
        var catalog = inv.AppCatalog;
        var all = refresh ? catalog.Refresh() : catalog.Apps;

        var filter = string.Join(' ', words);
        var apps = all
            .Where(a => filter.Length == 0
                || a.Name.Contains(filter, StringComparison.OrdinalIgnoreCase)
                || a.Executable.Contains(filter, StringComparison.OrdinalIgnoreCase))
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (apps.Count == 0)
        {
            inv.Output.Message(filter.Length == 0 ? "No apps found." : $"No app matches '{filter}'.");
        }

        var rows = apps.Select(a => new[] { a.Name, a.Executable }).ToList();
        inv.Output.Table("apps", ["name", "exe"], rows);
        return ExitCodes.Ok;
    }
}

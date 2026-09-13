using Wctl.Cli;

namespace Wctl.Commands.Apps;

public static class AppsCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "apps",
        Group = Groups.Apps,
        Summary = "List installed apps, optionally filtered",
        Usage = "apps [<filter>]",
        Details = "Shows the names 'w open' understands. The exe column is the short name for desktop apps: 'w open code'.",
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        var filter = string.Join(' ', inv.Args);
        var apps = inv.Services.Shell.InstalledApps()
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

using Wctl.Cli;
using Wctl.Platform;

namespace Wctl.Commands.Apps;

public static class OpenCommand
{
    public static readonly CommandSpec Spec = new()
    {
        Name = "open",
        Aliases = ["o"],
        Group = Groups.Apps,
        Summary = "Open an app, file, folder or link",
        Usage = "open <app | file | folder | link> [arguments]",
        Details = "'w spotify' is short for 'w open spotify'. Apps are matched by name like in the Start menu, so 'w open code' "
            + "and 'w open visual studio code' both work. Extra words go to the app: 'w open code .'. "
            + "Files and links open with their default app. 'w open settings' opens Windows Settings.",
        Run = Run,
    };

    private static int Run(Invocation inv)
    {
        if (inv.Args.Count == 0)
        {
            throw new WctlException($"What should be opened? Usage: w {Spec.Usage}");
        }

        var shell = inv.Services.Shell;
        var thing = inv.Args[0];

        if (thing.Equals("settings", StringComparison.OrdinalIgnoreCase))
        {
            shell.Open("ms-settings:");
            inv.Output.Action("Settings", "opened");
            return ExitCodes.Ok;
        }

        if (Urls.TryNormalize(thing, out var url))
        {
            shell.Open(url);
            inv.Output.State("Link", url);
            return ExitCodes.Ok;
        }

        var fullPath = Paths.Resolve(thing, shell.CurrentDirectory, shell.HomeDirectory);
        var kind = shell.Classify(fullPath);
        if (kind != PathKind.Missing || Paths.LooksLikePath(thing))
        {
            if (kind == PathKind.Missing)
            {
                throw new WctlException($"There is no file or folder at '{fullPath}'.");
            }

            shell.Open(fullPath);
            inv.Output.State(kind == PathKind.Directory ? "Folder" : "File", fullPath);
            return ExitCodes.Ok;
        }

        var app = AppMatcher.Find(shell.InstalledApps(), thing)
            ?? throw new WctlException($"Nothing called '{thing}' was found: not an installed app, file, folder or link. Run 'w apps {thing}' to search installed apps.");

        var arguments = inv.Args.Count > 1 ? Quote(inv.Args.Skip(1)) : null;
        shell.Open(app.Id, arguments);
        inv.Output.Action(app.Name, arguments is null ? "opened" : $"opened with {arguments}");
        return ExitCodes.Ok;
    }

    /// <summary>Puts quotes back around words with spaces, so 'w open code "my folder"' reaches the app as one argument.</summary>
    private static string Quote(IEnumerable<string> words)
        => string.Join(' ', words.Select(w => w.Contains(' ', StringComparison.Ordinal) ? $"\"{w}\"" : w));
}

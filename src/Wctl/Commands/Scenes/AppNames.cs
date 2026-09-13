using Wctl.Commands.Apps;
using Wctl.Platform;

namespace Wctl.Commands.Scenes;

/// <summary>Finds the word to put after 'open' so a scene starts the same app the window belongs to.</summary>
internal static class AppNames
{
    public static string ForOpen(WindowInfo window, IReadOnlyList<AppEntry> apps)
    {
        try
        {
            if (AppMatcher.Find(apps, window.App) is not null)
            {
                return window.App;
            }
        }
        catch (WctlException)
        {
            // Ambiguous by process name. Try the package below.
        }

        // Store apps: the process name (WindowsTerminal) differs from the app name (Terminal). The package family in the
        // WindowsApps folder name also appears in the app's id, so it links the two.
        var family = PackageFamily(window.ExecutablePath);
        if (family is not null)
        {
            var app = apps.FirstOrDefault(a => a.Id.Contains($"\\{family}!", StringComparison.OrdinalIgnoreCase));
            if (app is not null)
            {
                return app.Name;
            }
        }

        return window.App;
    }

    /// <summary>"C:\...\WindowsApps\Microsoft.WindowsTerminal_1.21.2911.0_x64__8wekyb3d8bbwe\WindowsTerminal.exe" gives "Microsoft.WindowsTerminal_8wekyb3d8bbwe".</summary>
    public static string? PackageFamily(string executablePath)
    {
        var parts = executablePath.Split('\\');
        var at = Array.FindIndex(parts, p => p.Equals("WindowsApps", StringComparison.OrdinalIgnoreCase));
        if (at < 0 || at + 1 >= parts.Length)
        {
            return null;
        }

        var folder = parts[at + 1];
        var first = folder.IndexOf('_', StringComparison.Ordinal);
        var last = folder.LastIndexOf('_');
        return first > 0 && last > first ? folder[..first] + "_" + folder[(last + 1)..] : null;
    }
}

using System.Runtime.InteropServices;

namespace Wctl.Platform.Windows;

/// <summary>Opening things and listing apps through the Windows shell.</summary>
public sealed class WindowsShell : IShell
{
    public string CurrentDirectory => Environment.CurrentDirectory;

    public string HomeDirectory => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    public PathKind Classify(string fullPath)
        => Directory.Exists(fullPath) ? PathKind.Directory
        : File.Exists(fullPath) ? PathKind.File
        : PathKind.Missing;

    public IReadOnlyList<AppEntry> InstalledApps()
    {
        // shell:AppsFolder is the virtual folder behind the Start menu's app list. It holds desktop and Store apps.
        Com.Initialize();
        var hr = Shell32.SHCreateItemFromParsingName("shell:AppsFolder", 0, in Shell32.IidShellItem, out var folderPointer);
        if (hr < 0)
        {
            Marshal.ThrowExceptionForHR(hr);
        }

        var folder = Com.Wrap<IShellItem>(folderPointer);
        var items = Com.Wrap<IEnumShellItems>(folder.BindToHandler(0, in Shell32.BhidEnumItems, in Shell32.IidEnumShellItems));

        var apps = new List<AppEntry>(256);
        while (true)
        {
            var next = items.Next(1, out var itemPointer, out var fetched);
            if (next < 0)
            {
                Marshal.ThrowExceptionForHR(next);
            }

            if (next != 0 || fetched == 0)
            {
                break;
            }

            var item = Com.Wrap<IShellItem>(itemPointer);
            var name = item.GetDisplayName(Shell32.SigdnNormalDisplay);
            var parsingName = item.GetDisplayName(Shell32.SigdnParentRelativeParsing);
            apps.Add(new AppEntry(name, @"shell:AppsFolder\" + parsingName, ExecutableOf(parsingName)));
        }

        return apps;
    }

    public void Open(string target, string? arguments = null)
    {
        var result = Shell32.ShellExecuteW(0, null, target, arguments, null, Shell32.SwShowNormal);
        if (result <= 32)
        {
            throw new WctlException($"Windows could not open '{target}': {Describe((int)result)}.");
        }
    }

    public void Reveal(string fullPath) => Open("explorer.exe", $"/select,\"{fullPath}\"");

    private static string ExecutableOf(string parsingName)
        => parsingName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(parsingName) : string.Empty;

    private static string Describe(int code) => code switch
    {
        0 or 8 => "out of memory",
        2 => "file not found",
        3 => "path not found",
        5 => "access denied",
        26 or 28 or 29 or 30 => "the app did not respond",
        31 => "no app is set up to open this kind of file",
        32 => "a required DLL is missing",
        _ => $"error {code}",
    };
}

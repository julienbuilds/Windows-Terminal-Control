using Wctl.Platform;

namespace Wctl.Tests;

internal sealed class FakeShell : IShell
{
    public const string SpotifyId = @"shell:AppsFolder\SpotifyAB.SpotifyMusic_zpdnekdrzrea0!Spotify";
    public const string CodeId = @"shell:AppsFolder\{6D809377-6AF0-444B-8957-A3773F02200E}\Microsoft VS Code\Code.exe";
    public const string NotepadId = @"shell:AppsFolder\Microsoft.WindowsNotepad_8wekyb3d8bbwe!App";

    public string CurrentDirectory { get; set; } = @"C:\Users\Julien\CursorProjects";

    public string HomeDirectory { get; set; } = @"C:\Users\Julien";

    public HashSet<string> Files { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        @"C:\Users\Julien\CursorProjects\notes.txt",
        @"C:\Users\Julien\CursorProjects\photo.png",
    };

    public HashSet<string> Directories { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        @"C:\Users\Julien",
        @"C:\Users\Julien\CursorProjects",
        @"C:\Users\Julien\CursorProjects\src",
        @"C:\Users\Julien\Downloads",
        @"C:\dev",
    };

    public List<AppEntry> Apps { get; } =
    [
        new("Spotify", SpotifyId, string.Empty),
        new("Visual Studio Code", CodeId, "Code"),
        new("Visual Studio 2022", @"shell:AppsFolder\{GUID}\VS\devenv.exe", "devenv"),
        new("Firefox", @"shell:AppsFolder\{GUID}\Mozilla Firefox\firefox.exe", "firefox"),
        new("Notepad", NotepadId, string.Empty),
        new("Steam", @"shell:AppsFolder\{GUID}\Steam\steam.exe", "steam"),
    ];

    public List<(string Target, string? Arguments)> Opened { get; } = [];

    public List<string> Revealed { get; } = [];

    public IReadOnlyList<AppEntry> InstalledApps() => Apps;

    public void Open(string target, string? arguments = null) => Opened.Add((target, arguments));

    public void Reveal(string fullPath) => Revealed.Add(fullPath);

    public PathKind Classify(string fullPath)
        => Directories.Contains(fullPath) ? PathKind.Directory
        : Files.Contains(fullPath) ? PathKind.File
        : PathKind.Missing;
}

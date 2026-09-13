namespace Wctl.Platform;

/// <summary>Opening things the way a double click does, plus the list of installed apps.</summary>
public interface IShell
{
    string CurrentDirectory { get; }

    string HomeDirectory { get; }

    /// <summary>Installed apps as the Start menu knows them: desktop apps and Store apps in one list.</summary>
    IReadOnlyList<AppEntry> InstalledApps();

    /// <summary>Opens a file, folder, link or app id with whatever Windows has for it.</summary>
    void Open(string target, string? arguments = null);

    /// <summary>Opens Explorer with the file or folder selected.</summary>
    void Reveal(string fullPath);

    PathKind Classify(string fullPath);
}

public enum PathKind
{
    Missing,
    File,
    Directory,
}

/// <param name="Name">Display name, for example "Visual Studio Code".</param>
/// <param name="Id">What Windows needs to start it. Passed to <see cref="IShell.Open"/>.</param>
/// <param name="Executable">Exe name without extension for desktop apps ("Code"), empty for Store apps.</param>
public sealed record AppEntry(string Name, string Id, string Executable);

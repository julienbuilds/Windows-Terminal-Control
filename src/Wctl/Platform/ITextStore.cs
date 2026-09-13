using System.Text;

namespace Wctl.Platform;

/// <summary>One text file wctl owns. Only reads and writes text; formats are handled by the callers.</summary>
public interface ITextStore
{
    string FilePath { get; }

    /// <returns>The file content, or null when there is no file yet.</returns>
    string? Read();

    void Write(string text);
}

public sealed class FileTextStore(string filePath) : ITextStore
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    public string FilePath { get; } = filePath;

    /// <summary>Scenes are Julien's own file, so they live in the roaming profile: %APPDATA%\wctl\scenes.txt</summary>
    public static ITextStore Scenes() => new FileTextStore(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wctl", "scenes.txt"));

    /// <summary>The app cache describes this PC and can be rebuilt any time, so it stays local: %LOCALAPPDATA%\wctl\apps.txt</summary>
    public static ITextStore AppCache() => new FileTextStore(
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "wctl", "apps.txt"));

    public string? Read() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;

    public void Write(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, text, Utf8NoBom);
    }
}

using System.Text;

namespace Wctl.Platform;

/// <summary>Where the scenes file lives. Only reads and writes text; the format is handled elsewhere.</summary>
public interface ISceneStore
{
    string FilePath { get; }

    /// <returns>The file content, or null when there is no file yet.</returns>
    string? Read();

    void Write(string text);
}

/// <summary>%APPDATA%\wctl\scenes.txt</summary>
public sealed class FileSceneStore : ISceneStore
{
    public string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "wctl", "scenes.txt");

    public string? Read() => File.Exists(FilePath) ? File.ReadAllText(FilePath) : null;

    public void Write(string text)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        File.WriteAllText(FilePath, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }
}

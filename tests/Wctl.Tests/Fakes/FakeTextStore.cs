using Wctl.Platform;

namespace Wctl.Tests;

internal sealed class FakeTextStore(string filePath) : ITextStore
{
    public string FilePath { get; } = filePath;

    /// <summary>Null means no file yet.</summary>
    public string? Text { get; set; }

    public List<string> Written { get; } = [];

    public string? Read() => Text;

    public void Write(string text)
    {
        Written.Add(text);
        Text = text;
    }
}

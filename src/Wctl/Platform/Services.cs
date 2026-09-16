using Wctl.Platform.Windows;

namespace Wctl.Platform;

/// <summary>
/// Everything that touches Windows, behind interfaces. Commands only see the interfaces,
/// so they can be tested with fakes and the real implementations can be checked by hand.
/// </summary>
public sealed class Services
{
    public required IAudio Audio { get; init; }

    public required IWindows Windows { get; init; }

    public required IShell Shell { get; init; }

    public required IMedia Media { get; init; }

    public required IDisplay Display { get; init; }

    public required IPower Power { get; init; }

    public required IClock Clock { get; init; }

    public required ITextStore Scenes { get; init; }

    public required ITextStore AppCache { get; init; }

    public required IPrompt Prompt { get; init; }

    /// <summary>The real thing. Construction is free; each implementation talks to Windows only when first used.</summary>
    public static Services Real() => new()
    {
        Audio = new CoreAudio(),
        Windows = new Win32Windows(),
        Shell = new WindowsShell(),
        Media = new WindowsMedia(),
        Display = new WindowsDisplay(),
        Power = new WindowsPower(),
        Clock = new SystemClock(),
        Scenes = FileTextStore.Scenes(),
        AppCache = FileTextStore.AppCache(),
        Prompt = new ConsolePrompt(),
    };
}

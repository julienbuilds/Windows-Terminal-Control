namespace Wctl.Platform;

/// <summary>Top level windows and monitors: list, move, show, focus, close, kill.</summary>
public interface IWindows
{
    /// <summary>The windows a person sees in Alt+Tab, frontmost first.</summary>
    IReadOnlyList<WindowInfo> List();

    /// <summary>All monitors, numbered the way Windows numbers them.</summary>
    IReadOnlyList<MonitorInfo> Monitors();

    /// <summary>The visible frame of the window in screen pixels, without the invisible resize border.</summary>
    Rect GetFrame(nint handle);

    /// <summary>Moves and resizes so that the visible frame matches <paramref name="frame"/>.</summary>
    void SetFrame(nint handle, Rect frame);

    void Show(nint handle, WindowState state);

    /// <returns>True when the window is in front afterwards. Windows may refuse.</returns>
    bool Focus(nint handle);

    /// <summary>Asks the window to close, like clicking its X. The app may ask to save first.</summary>
    void Close(nint handle);

    /// <summary>Terminates the process and everything it started.</summary>
    void Kill(uint processId);

    /// <summary>Terminates every process with that name (without .exe). Returns how many.</summary>
    int KillByName(string processName);

    void SetTopMost(nint handle, bool enabled);
}

public enum WindowState
{
    Restored,
    Minimized,
    Maximized,
}

/// <param name="Handle">Native window handle.</param>
/// <param name="Title">Window title as shown in the title bar.</param>
/// <param name="App">Process name without .exe, for example "firefox".</param>
/// <param name="ProcessId">Owning process.</param>
/// <param name="Monitor">Monitor number the window is on, or 0 when unknown.</param>
/// <param name="Active">Whether this is the foreground window.</param>
public sealed record WindowInfo(
    nint Handle,
    string Title,
    string App,
    uint ProcessId,
    int Monitor,
    bool Minimized,
    bool Maximized,
    bool TopMost,
    bool Active);

/// <param name="Index">1 based number, as Windows shows it in display settings.</param>
/// <param name="Device">Device name, for example \\.\DISPLAY1.</param>
/// <param name="Bounds">Full monitor area in screen pixels.</param>
/// <param name="WorkArea">Area without the taskbar.</param>
public sealed record MonitorInfo(int Index, string Device, Rect Bounds, Rect WorkArea, bool Primary);

/// <summary>A rectangle in screen pixels. Right and Bottom are exclusive.</summary>
public readonly record struct Rect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;

    public static Rect FromSize(int left, int top, int width, int height) => new(left, top, left + width, top + height);

    public bool Contains(Rect other) => other.Left >= Left && other.Top >= Top && other.Right <= Right && other.Bottom <= Bottom;

    public override string ToString() => $"{Left},{Top} {Width}x{Height}";
}

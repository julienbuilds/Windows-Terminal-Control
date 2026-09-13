using Wctl.Platform;

namespace Wctl.Tests;

/// <summary>Two monitors like Julien's desk and five windows. Every action is recorded for assertions.</summary>
internal sealed class FakeWindows : IWindows
{
    public const nint Firefox = 1;
    public const nint Discord = 2;
    public const nint Terminal = 3;
    public const nint Spotify = 4;
    public const nint FirefoxSecond = 5;

    public static readonly MonitorInfo Monitor1 = new(1, @"\\.\DISPLAY1", new Rect(0, 0, 3072, 1728), new Rect(0, 0, 3072, 1680), Primary: true);
    public static readonly MonitorInfo Monitor2 = new(2, @"\\.\DISPLAY2", new Rect(-2560, 272, 0, 1712), new Rect(-2560, 272, 0, 1664), Primary: false);

    public List<MonitorInfo> MonitorList { get; } = [Monitor1, Monitor2];

    public List<WindowInfo> WindowList { get; } =
    [
        new(Firefox, "ChatGPT - Mozilla Firefox", "firefox", 100, 1, Minimized: false, Maximized: false, TopMost: false, Active: true),
        new(Discord, "Discord", "Discord", 200, 2, Minimized: false, Maximized: true, TopMost: false, Active: false),
        new(Terminal, "PowerShell", "WindowsTerminal", 300, 1, Minimized: false, Maximized: false, TopMost: true, Active: false)
        {
            ExecutablePath = @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.2911.0_x64__8wekyb3d8bbwe\WindowsTerminal.exe",
        },
        new(Spotify, "Spotify Premium", "Spotify", 400, 1, Minimized: true, Maximized: false, TopMost: false, Active: false),
        new(FirefoxSecond, "Discord invite - Mozilla Firefox", "firefox", 100, 2, Minimized: false, Maximized: false, TopMost: false, Active: false),
    ];

    public Dictionary<nint, Rect> Frames { get; } = new()
    {
        [Firefox] = Rect.FromSize(100, 100, 1200, 800),
        [Discord] = Rect.FromSize(-2560, 272, 2560, 1392),
        [Terminal] = Rect.FromSize(1500, 200, 1000, 600),
        [Spotify] = Rect.FromSize(200, 300, 900, 700),
        [FirefoxSecond] = Rect.FromSize(-2000, 400, 1000, 800),
    };

    public List<(nint Handle, Rect Frame)> SetFrames { get; } = [];

    public List<(nint Handle, WindowState State)> Shown { get; } = [];

    public List<nint> Focused { get; } = [];

    public List<nint> Closed { get; } = [];

    public List<uint> Killed { get; } = [];

    public List<string> KilledByName { get; } = [];

    public Dictionary<string, int> RunningByName { get; } = new(StringComparer.OrdinalIgnoreCase) { ["steam"] = 2 };

    public List<(nint Handle, bool On)> TopMostChanges { get; } = [];

    public bool FocusSucceeds { get; set; } = true;

    public int ListCalls { get; private set; }

    /// <summary>Windows that show up on the given call of <see cref="List"/>, like an app that is still starting.</summary>
    public List<(int AtCall, WindowInfo Window)> Appearing { get; } = [];

    public IReadOnlyList<WindowInfo> List()
    {
        ListCalls++;
        foreach (var (_, window) in Appearing.Where(a => a.AtCall <= ListCalls && !WindowList.Contains(a.Window)).ToList())
        {
            WindowList.Add(window);
        }

        return WindowList;
    }

    public IReadOnlyList<MonitorInfo> Monitors() => MonitorList;

    public Rect GetFrame(nint handle) => Frames[handle];

    public void SetFrame(nint handle, Rect frame)
    {
        SetFrames.Add((handle, frame));
        Frames[handle] = frame;
    }

    public void Show(nint handle, WindowState state) => Shown.Add((handle, state));

    public bool Focus(nint handle)
    {
        Focused.Add(handle);
        return FocusSucceeds;
    }

    public void Close(nint handle) => Closed.Add(handle);

    public void Kill(uint processId) => Killed.Add(processId);

    public int KillByName(string processName)
    {
        KilledByName.Add(processName);
        return RunningByName.GetValueOrDefault(processName);
    }

    public void SetTopMost(nint handle, bool enabled) => TopMostChanges.Add((handle, enabled));
}

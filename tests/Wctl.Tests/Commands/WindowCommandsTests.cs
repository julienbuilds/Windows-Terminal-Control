using System.Text.Json;
using Wctl.Platform;

namespace Wctl.Tests.Commands;

public class WindowCommandsTests
{
    [Theory]
    [InlineData("ls")]
    [InlineData("windows")]
    public void Ls_ListsWindowsWithNumbersAndMarksTheActiveOne(string command)
    {
        var result = TestHost.Run(command);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var lines = result.Stdout.Split('\n');
        var firefox = Assert.Single(lines, l => l.Contains("ChatGPT", StringComparison.Ordinal));
        Assert.Contains("●", firefox);
        Assert.Contains("│ 1 ", firefox);
        Assert.DoesNotContain("●", Assert.Single(lines, l => l.Contains("Spotify Premium", StringComparison.Ordinal)));
    }

    [Fact]
    public void Ls_Json_HasIndexAppTitleMonitor()
    {
        var result = TestHost.Run("ls", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        var windows = doc.RootElement.GetProperty("windows").EnumerateArray().ToList();
        Assert.Equal(5, windows.Count);
        Assert.Equal("2", windows[1].GetProperty("index").GetString());
        Assert.Equal("Discord", windows[1].GetProperty("app").GetString());
        Assert.Equal("Discord", windows[1].GetProperty("title").GetString());
        Assert.Equal("2", windows[1].GetProperty("monitor").GetString());
        Assert.True(windows[0].GetProperty("current").GetBoolean());
        Assert.False(windows[1].GetProperty("current").GetBoolean());
    }

    [Fact]
    public void Ls_WithoutWindows_SaysSo()
    {
        var fakes = new Fakes();
        fakes.Windows.WindowList.Clear();

        var result = TestHost.Run(fakes, "ls");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal("No windows open.\n", result.Stdout);
    }

    [Fact]
    public void Ls_ShortensLongTitles()
    {
        var fakes = new Fakes();
        fakes.Windows.WindowList.Add(new(9, new string('x', 100), "app", 900, 1, false, false, false, false));

        var result = TestHost.Run(fakes, "ls");

        Assert.Contains(new string('x', 59) + "…", result.Stdout);
        Assert.DoesNotContain(new string('x', 60), result.Stdout);
    }

    [Fact]
    public void Monitors_ListsThemAndMarksThePrimary()
    {
        var result = TestHost.Run("monitors");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var lines = result.Stdout.Split('\n');
        Assert.Contains("●", Assert.Single(lines, l => l.Contains("3072x1728", StringComparison.Ordinal)));
        Assert.DoesNotContain("●", Assert.Single(lines, l => l.Contains("2560x1440", StringComparison.Ordinal)));
        Assert.Contains("-2560,272", result.Stdout);
    }

    [Theory]
    [InlineData("left", 0, 1536)]
    [InlineData("l", 0, 1536)]
    [InlineData("right", 1536, 3072)]
    [InlineData("r", 1536, 3072)]
    public void Move_Half_SnapsToTheWorkAreaOfItsMonitor(string side, int left, int right)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "move", "firefox", side);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var (handle, frame) = Assert.Single(fakes.Windows.SetFrames);
        Assert.Equal(FakeWindows.Firefox, handle);
        Assert.Equal(new Rect(left, 0, right, 1680), frame);
        Assert.Equal($"firefox: {(left == 0 ? "left" : "right")} half of monitor 1\n", result.Stdout);
    }

    [Theory]
    [InlineData("m")]
    [InlineData("snap")]
    public void Move_Aliases(string alias)
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, alias, "firefox", "left");

        Assert.Single(fakes.Windows.SetFrames);
    }

    [Fact]
    public void Move_UsesTheMonitorTheWindowIsOn()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "move", "5", "left");

        var (_, frame) = Assert.Single(fakes.Windows.SetFrames);
        Assert.Equal(new Rect(-2560, 272, -1280, 1664), frame);
    }

    [Fact]
    public void Move_RestoresAMaximizedWindowFirst()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "move", "discord", "left");

        Assert.Equal((FakeWindows.Discord, WindowState.Restored), Assert.Single(fakes.Windows.Shown));
    }

    [Fact]
    public void Move_RestoresAMinimizedWindowFirst()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "move", "spotify", "right");

        Assert.Equal((FakeWindows.Spotify, WindowState.Restored), Assert.Single(fakes.Windows.Shown));
    }

    [Theory]
    [InlineData("2")]
    [InlineData("monitor", "2")]
    public void Move_ToMonitor_KeepsTheOffsetAndSize(params string[] where)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, ["move", "firefox", .. where]);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var (_, frame) = Assert.Single(fakes.Windows.SetFrames);
        Assert.Equal(Rect.FromSize(-2460, 372, 1200, 800), frame);
        Assert.Equal("firefox: monitor 2\n", result.Stdout);
        Assert.Empty(fakes.Windows.Shown);
    }

    [Fact]
    public void Move_ToMonitor_MaximizedWindow_IsMaximizedAgainThere()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "move", "discord", "1");

        Assert.Equal(
            [(FakeWindows.Discord, WindowState.Restored), (FakeWindows.Discord, WindowState.Maximized)],
            fakes.Windows.Shown);
        var (_, frame) = Assert.Single(fakes.Windows.SetFrames);
        Assert.True(FakeWindows.Monitor1.WorkArea.Contains(frame));
    }

    [Fact]
    public void Move_ToUnknownMonitor_Fails()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "move", "firefox", "3");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("There is no monitor 3", result.Stderr);
        Assert.Empty(fakes.Windows.SetFrames);
    }

    [Fact]
    public void Move_Exact_PlacesTheFrame()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "move", "firefox", "-100", "50", "1200", "800");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var (_, frame) = Assert.Single(fakes.Windows.SetFrames);
        Assert.Equal(Rect.FromSize(-100, 50, 1200, 800), frame);
        Assert.Equal("firefox: -100,50 1200x800\n", result.Stdout);
    }

    [Theory]
    [InlineData("firefox")]
    [InlineData("firefox", "up")]
    [InlineData("firefox", "100", "100")]
    [InlineData("firefox", "100", "100", "0", "800")]
    [InlineData("firefox", "a", "b", "c", "d")]
    public void Move_WithBadPlacement_Fails(params string[] argv)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, ["move", .. argv]);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Empty(fakes.Windows.SetFrames);
    }

    [Fact]
    public void Move_WithoutWindow_Fails()
    {
        var result = TestHost.Run("move");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Which window? Usage: w move", result.Stderr);
    }

    [Fact]
    public void Center_CentersOnItsMonitorKeepingSize()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "center", "firefox");

        var (_, frame) = Assert.Single(fakes.Windows.SetFrames);
        Assert.Equal(Rect.FromSize(936, 440, 1200, 800), frame);
        Assert.Equal("firefox: centered on monitor 1\n", result.Stdout);
    }

    [Theory]
    [InlineData("max", WindowState.Maximized, "maximized")]
    [InlineData("maximize", WindowState.Maximized, "maximized")]
    [InlineData("min", WindowState.Minimized, "minimized")]
    [InlineData("minimize", WindowState.Minimized, "minimized")]
    public void MaxAndMin_ChangeTheWindowState(string command, WindowState expected, string word)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command, "firefox");

        Assert.Equal((FakeWindows.Firefox, expected), Assert.Single(fakes.Windows.Shown));
        Assert.Equal($"firefox: {word}\n", result.Stdout);
    }

    [Theory]
    [InlineData("max")]
    [InlineData("min")]
    public void MaxAndMin_Off_Restores(string command)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command, "firefox", "off");

        Assert.Equal((FakeWindows.Firefox, WindowState.Restored), Assert.Single(fakes.Windows.Shown));
        Assert.Equal("firefox: restored\n", result.Stdout);
    }

    [Fact]
    public void Max_WithUnknownWord_Fails()
    {
        var result = TestHost.Run("max", "firefox", "please");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Got 'please'", result.Stderr);
    }

    [Fact]
    public void Top_Toggles()
    {
        var fakes = new Fakes();

        var on = TestHost.Run(fakes, "top", "firefox");
        var off = TestHost.Run(fakes, "top", "terminal");

        Assert.Equal([(FakeWindows.Firefox, true), (FakeWindows.Terminal, false)], fakes.Windows.TopMostChanges);
        Assert.Equal("firefox: always on top\n", on.Stdout);
        Assert.Equal("WindowsTerminal: no longer on top\n", off.Stdout);
    }

    [Theory]
    [InlineData("on", true)]
    [InlineData("off", false)]
    public void Top_OnAndOff_AreExplicit(string word, bool expected)
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "top", "firefox", word);

        Assert.Equal((FakeWindows.Firefox, expected), Assert.Single(fakes.Windows.TopMostChanges));
    }

    [Theory]
    [InlineData("focus")]
    [InlineData("f")]
    public void Focus_BringsTheFrontmostMatchToTheFront(string command)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command, "firefox");

        Assert.Equal(FakeWindows.Firefox, Assert.Single(fakes.Windows.Focused));
        Assert.Equal("firefox: focused\n", result.Stdout);
    }

    [Fact]
    public void Focus_WhenWindowsRefuses_Fails()
    {
        var fakes = new Fakes { Windows = { FocusSucceeds = false } };

        var result = TestHost.Run(fakes, "focus", "firefox");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("did not let 'firefox' come to the front", result.Stderr);
    }

    [Theory]
    [InlineData("close")]
    [InlineData("x")]
    public void Close_ByApp_ClosesAllItsWindows(string command)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command, "firefox");

        Assert.Equal([FakeWindows.Firefox, FakeWindows.FirefoxSecond], fakes.Windows.Closed);
        Assert.Equal("firefox: closing 2 windows\n", result.Stdout);
    }

    [Fact]
    public void Close_ByTitleOrNumber_ClosesOneWindow()
    {
        var fakes = new Fakes();

        var byTitle = TestHost.Run(fakes, "close", "invite");
        var byNumber = TestHost.Run(fakes, "close", "2");

        Assert.Equal([FakeWindows.FirefoxSecond, FakeWindows.Discord], fakes.Windows.Closed);
        Assert.Equal("firefox: closing\n", byTitle.Stdout);
        Assert.Equal("Discord: closing\n", byNumber.Stdout);
    }

    [Fact]
    public void Kill_ByApp_KillsEachProcessOnce()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "kill", "firefox");

        Assert.Equal([100u], fakes.Windows.Killed);
        Assert.Equal("firefox: killed\n", result.Stdout);
    }

    [Fact]
    public void Kill_WithoutWindow_FallsBackToProcessName()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "kill", "steam.exe");

        Assert.Empty(fakes.Windows.Killed);
        Assert.Equal("steam", Assert.Single(fakes.Windows.KilledByName));
        Assert.Equal("steam: killed 2 processes\n", result.Stdout);
    }

    [Fact]
    public void Kill_NothingRunning_Fails()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "kill", "blender");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Nothing named 'blender' is running", result.Stderr);
    }

    [Fact]
    public void Action_Json_HasTargetAndResult()
    {
        var result = TestHost.Run("move", "firefox", "left", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal("firefox", doc.RootElement.GetProperty("target").GetString());
        Assert.Equal("left half of monitor 1", doc.RootElement.GetProperty("result").GetString());
    }
}

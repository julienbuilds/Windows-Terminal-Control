using Wctl.Commands.Windows;

namespace Wctl.Tests.Commands;

public class WindowTargetTests
{
    private readonly FakeWindows windows = new();

    [Theory]
    [InlineData("1", 1)]
    [InlineData("3", 3)]
    [InlineData("5", 5)]
    public void Number_PicksThatWindowOnly(string query, int expected)
    {
        var match = WindowTarget.Resolve(windows.WindowList, query);

        Assert.Equal((nint)expected, Assert.Single(match.Windows).Handle);
        Assert.False(match.ByApp);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("6")]
    public void Number_OutOfRange_Fails(string query)
    {
        var e = Assert.Throws<WctlException>(() => WindowTarget.Resolve(windows.WindowList, query));

        Assert.Contains("no window number", e.Message);
    }

    [Theory]
    [InlineData("firefox")]
    [InlineData("Firefox")]
    [InlineData("firefox.exe")]
    [InlineData("  firefox ")]
    public void ExactAppName_MatchesAllWindowsOfThatApp(string query)
    {
        var match = WindowTarget.Resolve(windows.WindowList, query);

        Assert.True(match.ByApp);
        Assert.Equal([FakeWindows.Firefox, FakeWindows.FirefoxSecond], match.Windows.Select(w => w.Handle));
        Assert.Equal("firefox", match.Label);
    }

    [Fact]
    public void ExactAppName_BeatsTitleMatches()
    {
        // "discord" is the Discord app, but also part of a Firefox window title.
        var match = WindowTarget.Resolve(windows.WindowList, "discord");

        Assert.True(match.ByApp);
        Assert.Equal(FakeWindows.Discord, Assert.Single(match.Windows).Handle);
    }

    [Fact]
    public void PartOfAppName_Matches()
    {
        var match = WindowTarget.Resolve(windows.WindowList, "termin");

        Assert.True(match.ByApp);
        Assert.Equal(FakeWindows.Terminal, Assert.Single(match.Windows).Handle);
    }

    [Fact]
    public void PartOfAppName_MatchingSeveralApps_Fails()
    {
        windows.WindowList.Add(new(9, "Other", "firebird", 900, 1, false, false, false, false));

        var e = Assert.Throws<WctlException>(() => WindowTarget.Resolve(windows.WindowList, "fire"));

        Assert.Contains("more than one app", e.Message);
        Assert.Contains("firefox", e.Message);
        Assert.Contains("firebird", e.Message);
    }

    [Fact]
    public void PartOfTitle_MatchesTheFrontmostWindowOnly()
    {
        var match = WindowTarget.Resolve(windows.WindowList, "chatgpt");

        Assert.False(match.ByApp);
        Assert.Equal(FakeWindows.Firefox, Assert.Single(match.Windows).Handle);
    }

    [Fact]
    public void Title_MatchingWindowsOfSeveralApps_Fails()
    {
        windows.WindowList.Add(new(9, "ChatGPT desktop", "OpenAI", 900, 1, false, false, false, false));

        var e = Assert.Throws<WctlException>(() => WindowTarget.Resolve(windows.WindowList, "chatgpt"));

        Assert.Contains("more than one app", e.Message);
        Assert.Contains("Use the number", e.Message);
    }

    [Fact]
    public void NoMatch_Fails()
    {
        var e = Assert.Throws<WindowNotFoundException>(() => WindowTarget.Resolve(windows.WindowList, "blender"));

        Assert.Contains("No window matches 'blender'", e.Message);
    }

    [Fact]
    public void Find_ReturnsNullInsteadOfFailingForNoMatch()
    {
        Assert.Null(WindowTarget.Find(windows.WindowList, "blender"));
    }

    [Fact]
    public void EmptyQuery_Fails()
    {
        var e = Assert.Throws<WctlException>(() => WindowTarget.Resolve(windows.WindowList, ".exe"));

        Assert.Contains("Which window?", e.Message);
    }
}

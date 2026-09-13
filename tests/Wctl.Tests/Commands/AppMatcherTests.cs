using Wctl.Commands.Apps;
using Wctl.Platform;

namespace Wctl.Tests.Commands;

public class AppMatcherTests
{
    private static readonly List<AppEntry> Apps =
    [
        new("Visual Studio Code", "code-id", "Code"),
        new("Visual Studio 2022", "vs-id", "devenv"),
        new("Code Writer", "writer-id", string.Empty),
        new("Steam", "steam-id", "steam"),
        new("Steam", "steam-dup-id", "steam"),
    ];

    [Fact]
    public void ExactName_BeatsEverything()
    {
        Assert.Equal("code-id", AppMatcher.Find(Apps, "visual studio code")?.Id);
    }

    [Fact]
    public void ExeName_BeatsPartialNames()
    {
        // "code" is the exe of Visual Studio Code and also the start of "Code Writer".
        Assert.Equal("code-id", AppMatcher.Find(Apps, "code")?.Id);
        Assert.Equal("code-id", AppMatcher.Find(Apps, "CODE.EXE")?.Id);
    }

    [Fact]
    public void StartOfName_Matches()
    {
        Assert.Equal("writer-id", AppMatcher.Find(Apps, "code w")?.Id);
    }

    [Fact]
    public void PartOfName_Matches()
    {
        Assert.Equal("writer-id", AppMatcher.Find(Apps, "writer")?.Id);
    }

    [Fact]
    public void SeveralAppsOnOneLevel_Fail()
    {
        var e = Assert.Throws<WctlException>(() => AppMatcher.Find(Apps, "visual"));

        Assert.Contains("Visual Studio Code, Visual Studio 2022", e.Message);
    }

    [Fact]
    public void DuplicatesWithTheSameName_AreNotAmbiguous()
    {
        Assert.Equal("steam-id", AppMatcher.Find(Apps, "steam")?.Id);
    }

    [Theory]
    [InlineData("blender")]
    [InlineData("")]
    [InlineData(".exe")]
    public void NothingFound_ReturnsNull(string query)
    {
        Assert.Null(AppMatcher.Find(Apps, query));
    }
}

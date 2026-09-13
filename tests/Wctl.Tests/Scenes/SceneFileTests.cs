using Wctl.Scenes;

namespace Wctl.Tests.Scenes;

public class SceneFileTests
{
    [Fact]
    public void Parse_ReadsScenesAndSteps()
    {
        var text = """
            # comment
            [work]
            hdr off
              vol 15

            open firefox
            [gaming]
            hdr on monitor main
            """;

        var scenes = SceneFile.Parse(text);

        Assert.Equal(2, scenes.Count);
        Assert.Equal("work", scenes[0].Name);
        Assert.Equal(["hdr off", "vol 15", "open firefox"], scenes[0].Steps);
        Assert.Equal("gaming", scenes[1].Name);
        Assert.Equal(["hdr on monitor main"], scenes[1].Steps);
    }

    [Fact]
    public void Parse_EmptyText_HasNoScenes()
    {
        Assert.Empty(SceneFile.Parse(string.Empty));
        Assert.Empty(SceneFile.Parse("# only a comment\n"));
    }

    [Fact]
    public void Parse_HandlesWindowsLineEndings()
    {
        var scenes = SceneFile.Parse("[work]\r\nvol 15\r\n");

        Assert.Equal(["vol 15"], Assert.Single(scenes).Steps);
    }

    [Theory]
    [InlineData("vol 15\n[work]\n", "line 1", "before any [scene] header")]
    [InlineData("[work\nvol 15\n", "line 1", "looks like [name]")]
    [InlineData("[my scene]\n", "line 1", "one word")]
    [InlineData("[work]\n[Work]\n", "line 2", "appears twice")]
    public void Parse_FailsWithTheLineNumber(string text, string line, string reason)
    {
        var e = Assert.Throws<WctlException>(() => SceneFile.Parse(text));

        Assert.Contains(line, e.Message);
        Assert.Contains(reason, e.Message);
    }

    [Fact]
    public void Format_ThenParse_RoundTrips()
    {
        var scenes = new List<Scene>
        {
            new("work", ["hdr off", "vol 15", "open code \"my folder\""]),
            new("gaming", []),
        };

        var text = SceneFile.Format(scenes);
        var parsed = SceneFile.Parse(text);

        Assert.Equal(scenes.Select(s => s.Name), parsed.Select(s => s.Name));
        Assert.Equal(scenes[0].Steps, parsed[0].Steps);
        Assert.Empty(parsed[1].Steps);
        Assert.StartsWith("# wctl scenes", text, StringComparison.Ordinal);
        Assert.Contains("\n[work]\nhdr off\nvol 15\nopen code \"my folder\"\n\n[gaming]\n", text);
    }

    [Theory]
    [InlineData("work")]
    [InlineData("Deep-Work_2")]
    public void ValidateName_AcceptsPlainWords(string name)
    {
        SceneFile.ValidateName(name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("my scene")]
    [InlineData("work!")]
    [InlineData("a/b")]
    public void ValidateName_RejectsEverythingElse(string name)
    {
        Assert.Throws<WctlException>(() => SceneFile.ValidateName(name));
    }
}

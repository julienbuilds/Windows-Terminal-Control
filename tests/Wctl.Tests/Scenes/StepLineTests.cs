using Wctl.Scenes;

namespace Wctl.Tests.Scenes;

public class StepLineTests
{
    [Theory]
    [InlineData("vol 15", new[] { "vol", "15" })]
    [InlineData("  move   firefox  left ", new[] { "move", "firefox", "left" })]
    [InlineData("open code \"my folder\" --wait", new[] { "open", "code", "my folder", "--wait" })]
    [InlineData("focus \"\"", new[] { "focus", "" })]
    [InlineData("say \"a b\"c", new[] { "say", "a bc" })]
    [InlineData("", new string[0])]
    public void Split_SeparatesWordsAndKeepsQuotedGroups(string line, string[] expected)
    {
        Assert.Equal(expected, StepLine.Split(line));
    }

    [Fact]
    public void Split_RejectsAnOpenQuote()
    {
        var e = Assert.Throws<WctlException>(() => StepLine.Split("open \"my folder"));

        Assert.Contains("quote is not closed", e.Message);
    }
}

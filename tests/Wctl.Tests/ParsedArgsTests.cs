using Wctl.Cli;

namespace Wctl.Tests;

public class ParsedArgsTests
{
    [Fact]
    public void NegativeNumbers_AreValuesNotOptions()
    {
        var parsed = ParsedArgs.Parse(["vol", "-5"]);

        Assert.Equal(new[] { "vol", "-5" }, parsed.Positionals);
        Assert.False(parsed.Help);
    }

    [Fact]
    public void GlobalFlags_AreExtractedFromAnyPosition()
    {
        var parsed = ParsedArgs.Parse(["--json", "hdr", "--debug", "-h", "--markdown", "-V"]);

        Assert.Equal(new[] { "hdr" }, parsed.Positionals);
        Assert.True(parsed.Json);
        Assert.True(parsed.Debug);
        Assert.True(parsed.Help);
        Assert.True(parsed.Markdown);
        Assert.True(parsed.Version);
    }

    [Fact]
    public void UnknownOption_Throws()
    {
        var e = Assert.Throws<WctlException>(() => ParsedArgs.Parse(["vol", "--loud"]));

        Assert.Contains("--loud", e.Message);
        Assert.Equal(ExitCodes.Failure, e.ExitCode);
    }
}

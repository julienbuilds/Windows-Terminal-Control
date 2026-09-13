using Wctl.Commands;

namespace Wctl.Tests.Commands;

public class LevelTests
{
    [Theory]
    [InlineData("+5", 47, 52)]
    [InlineData("-5", 47, 42)]
    [InlineData("+100", 47, 100)]
    [InlineData("-100", 47, 0)]
    [InlineData("0", 47, 0)]
    [InlineData("100", 47, 100)]
    [InlineData("007", 47, 7)]
    public void Apply_ComputesTheNewLevel(string arg, int current, int expected)
    {
        Assert.Equal(expected, Level.Apply(arg, current, "Volume"));
    }

    [Theory]
    [InlineData("101")]
    [InlineData("abc")]
    [InlineData("+")]
    [InlineData("-")]
    [InlineData("++5")]
    [InlineData("5%")]
    [InlineData("1.5")]
    [InlineData("")]
    public void Apply_RejectsBadInput(string arg)
    {
        var e = Assert.Throws<WctlException>(() => Level.Apply(arg, 47, "Volume"));

        Assert.Contains(arg.Length == 0 ? "Got ''" : arg, e.Message);
    }
}

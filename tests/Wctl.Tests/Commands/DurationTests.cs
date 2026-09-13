using Wctl.Commands.Power;

namespace Wctl.Tests.Commands;

public class DurationTests
{
    [Theory]
    [InlineData("90m", 90 * 60)]
    [InlineData("90", 90 * 60)]
    [InlineData("1h", 3600)]
    [InlineData("1h30m", 5400)]
    [InlineData("1H30M", 5400)]
    [InlineData("45s", 45)]
    [InlineData("2h5m10s", 7510)]
    [InlineData(" 10m ", 600)]
    public void Parse_ReadsHoursMinutesSeconds(string text, int seconds)
    {
        Assert.Equal(TimeSpan.FromSeconds(seconds), Duration.Parse(text));
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("5x")]
    [InlineData("m5")]
    [InlineData("1h30")]
    [InlineData("-5m")]
    [InlineData("1.5h")]
    [InlineData("1234567s")]
    public void Parse_RejectsWhatItDoesNotUnderstand(string text)
    {
        var e = Assert.Throws<WctlException>(() => Duration.Parse(text));

        Assert.Contains("Expected a duration", e.Message);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("0m")]
    [InlineData("0h0s")]
    public void Parse_RejectsZero(string text)
    {
        var e = Assert.Throws<WctlException>(() => Duration.Parse(text));

        Assert.Contains("more than zero", e.Message);
    }

    [Theory]
    [InlineData(0, "0s")]
    [InlineData(45, "45s")]
    [InlineData(60, "1m")]
    [InlineData(125, "2m 5s")]
    [InlineData(3600, "1h")]
    [InlineData(5400, "1h 30m")]
    [InlineData(3661, "1h 1m 1s")]
    public void Format_UsesHoursMinutesSeconds(int seconds, string expected)
    {
        Assert.Equal(expected, Duration.Format(TimeSpan.FromSeconds(seconds)));
    }

    [Fact]
    public void Format_RoundsToWholeSeconds()
    {
        Assert.Equal("3s", Duration.Format(TimeSpan.FromMilliseconds(2600)));
    }
}

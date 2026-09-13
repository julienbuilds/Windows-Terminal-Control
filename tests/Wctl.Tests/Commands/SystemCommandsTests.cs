using System.Text.Json;

namespace Wctl.Tests.Commands;

public class SystemCommandsTests
{
    [Fact]
    public void Lock_LocksTheScreen()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "lock");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(1, fakes.Power.Locked);
        Assert.Equal("PC: locked\n", result.Stdout);
    }

    [Fact]
    public void Sleep_PutsThePcToSleep()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "sleep");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(1, fakes.Power.Slept);
        Assert.Equal("PC: sleeping\n", result.Stdout);
    }

    [Theory]
    [InlineData("lock")]
    [InlineData("sleep")]
    public void LockAndSleep_RejectArguments(string command)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command, "now");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(0, fakes.Power.Locked + fakes.Power.Slept);
    }

    [Fact]
    public void Awake_HoldsTheRequestForTheDurationThenReleasesIt()
    {
        var fakes = new Fakes();
        var start = fakes.Clock.Now;

        var result = TestHost.Run(fakes, "awake", "3s");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(1, fakes.Power.AwakeAcquired);
        Assert.Equal(1, fakes.Power.AwakeReleased);
        Assert.Equal(start + TimeSpan.FromSeconds(3), fakes.Clock.Now);
        Assert.Equal(3, fakes.Clock.Delays);
        Assert.Contains("Awake for another 3s", result.Stdout);
        Assert.EndsWith("Kept awake: 3s\n", result.Stdout);
    }

    [Fact]
    public void Awake_CountsDownInOneSecondSteps()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "awake", "2m");

        Assert.Equal(120, fakes.Clock.Delays);
        Assert.Contains("Awake for another 2m", result.Stdout);
        Assert.Contains("Awake for another 1m 59s", result.Stdout);
        Assert.Contains("Awake for another 1s", result.Stdout);
        Assert.EndsWith("Kept awake: 2m\n", result.Stdout);
    }

    [Fact]
    public void Awake_StoppedEarly_SaysSoAndStillReleases()
    {
        var fakes = new Fakes();
        fakes.Clock.StopAt = fakes.Clock.Now + TimeSpan.FromSeconds(10);

        var result = TestHost.Run(fakes, "awake", "1h");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(1, fakes.Power.AwakeReleased);
        Assert.EndsWith("Kept awake: 10s (stopped early)\n", result.Stdout);
    }

    [Fact]
    public void Awake_WithoutDuration_RunsUntilStopped()
    {
        var fakes = new Fakes();
        fakes.Clock.StopAt = fakes.Clock.Now + TimeSpan.FromMinutes(5);

        var result = TestHost.Run(fakes, "awake");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains("Awake until you press Ctrl+C", result.Stdout);
        Assert.EndsWith("Kept awake: 5m\n", result.Stdout);
        Assert.Equal(1, fakes.Power.AwakeReleased);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0m")]
    [InlineData("-5m")]
    [InlineData("5x")]
    public void Awake_RejectsBadDurations(string duration)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "awake", duration);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(0, fakes.Power.AwakeAcquired);
    }

    [Fact]
    public void Awake_Json_HasOnlyTheSummary()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "awake", "2s", "--json");

        Assert.Equal(string.Empty, result.Stdout);
        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal("2s", doc.RootElement.GetProperty("kept_awake").GetString());
    }
}

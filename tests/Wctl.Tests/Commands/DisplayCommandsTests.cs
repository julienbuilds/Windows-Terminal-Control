using System.Text.Json;
using Wctl.Platform;

namespace Wctl.Tests.Commands;

public class DisplayCommandsTests
{
    [Theory]
    [InlineData("hdr")]
    [InlineData("h")]
    public void Hdr_Toggles(string command)
    {
        var fakes = new Fakes();

        var on = TestHost.Run(fakes, command);
        var off = TestHost.Run(fakes, command);

        Assert.Equal([(FakeDisplay.Display1, true), (FakeDisplay.Display1, false)], fakes.Display.HdrChanges);
        Assert.Equal("HDR: on\n", on.Stdout);
        Assert.Equal("HDR: off\n", off.Stdout);
    }

    [Fact]
    public void Hdr_OnlyTouchesDisplaysThatSupportIt()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "hdr", "on");

        Assert.DoesNotContain(fakes.Display.HdrChanges, c => c.Device == FakeDisplay.Display2);
    }

    [Theory]
    [InlineData("on", true)]
    [InlineData("off", false)]
    public void Hdr_OnAndOff_AreExplicitAndIdempotent(string word, bool expected)
    {
        var fakes = new Fakes();
        fakes.Display.Hdr[0] = fakes.Display.Hdr[0] with { Enabled = !expected };

        TestHost.Run(fakes, "hdr", word);
        var second = TestHost.Run(fakes, "hdr", word);

        Assert.Equal((FakeDisplay.Display1, expected), Assert.Single(fakes.Display.HdrChanges));
        Assert.Equal(expected, fakes.Display.Hdr[0].Enabled);
        Assert.Equal($"HDR: {(expected ? "on" : "off")}\n", second.Stdout);
    }

    [Fact]
    public void Hdr_Status_ChangesNothing()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "hdr", "status");

        Assert.Empty(fakes.Display.HdrChanges);
        Assert.Equal("HDR: off\n", result.Stdout);
    }

    [Fact]
    public void Hdr_WithSeveralCapableDisplays_TurnsAllOnWhenMixed()
    {
        var fakes = new Fakes();
        fakes.Display.Hdr[0] = fakes.Display.Hdr[0] with { Enabled = true };
        fakes.Display.Hdr[1] = new HdrDisplay(FakeDisplay.Display2, Supported: true, Enabled: false);

        var result = TestHost.Run(fakes, "hdr");

        Assert.Equal((FakeDisplay.Display2, true), Assert.Single(fakes.Display.HdrChanges));
        Assert.Contains("HDR: on", result.Stdout);
        Assert.Contains("│ 1 ", result.Stdout);
        Assert.Contains("│ 2 ", result.Stdout);
    }

    [Fact]
    public void Hdr_Status_WithMixedDisplays_SaysMixedAndListsThem()
    {
        var fakes = new Fakes();
        fakes.Display.Hdr[0] = fakes.Display.Hdr[0] with { Enabled = true };
        fakes.Display.Hdr[1] = new HdrDisplay(FakeDisplay.Display2, Supported: true, Enabled: false);

        var result = TestHost.Run(fakes, "hdr", "status");

        Assert.Contains("HDR: mixed", result.Stdout);
        var lines = result.Stdout.Split('\n');
        Assert.Contains("on", Assert.Single(lines, l => l.Contains("│ 1 ", StringComparison.Ordinal)));
        Assert.Contains("off", Assert.Single(lines, l => l.Contains("│ 2 ", StringComparison.Ordinal)));
    }

    [Fact]
    public void Hdr_WithoutCapableDisplay_Fails()
    {
        var fakes = new Fakes();
        fakes.Display.Hdr[0] = fakes.Display.Hdr[0] with { Supported = false };

        var result = TestHost.Run(fakes, "hdr");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("No display supports HDR", result.Stderr);
        Assert.Empty(fakes.Display.HdrChanges);
    }

    [Fact]
    public void Hdr_RejectsUnknownWords()
    {
        var result = TestHost.Run("hdr", "maybe");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Got 'maybe'", result.Stderr);
    }

    [Fact]
    public void Hdr_Json_IsABoolean()
    {
        var result = TestHost.Run("hdr", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.True(doc.RootElement.GetProperty("hdr").GetBoolean());
    }

    [Theory]
    [InlineData("brightness")]
    [InlineData("bright")]
    [InlineData("brightness", "status")]
    public void Brightness_ShowsEveryMonitor(params string[] argv)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Empty(fakes.Display.BrightnessChanges);
        var lines = result.Stdout.Split('\n');
        Assert.Contains("60%", Assert.Single(lines, l => l.Contains("│ 1 ", StringComparison.Ordinal)));
        Assert.Contains("40%", Assert.Single(lines, l => l.Contains("│ 2 ", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("+10", 70, 50)]
    [InlineData("-50", 10, 0)]
    [InlineData("30", 30, 30)]
    [InlineData("100", 100, 100)]
    public void Brightness_SetsEveryMonitor(string arg, int first, int second)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "brightness", arg);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal([(FakeDisplay.Display1, first), (FakeDisplay.Display2, second)], fakes.Display.BrightnessChanges);
        Assert.Contains($"{first}%", result.Stdout);
        Assert.Contains($"{second}%", result.Stdout);
    }

    [Fact]
    public void Brightness_WithOneMonitor_PrintsOneLine()
    {
        var fakes = new Fakes();
        fakes.Display.Brightness[1] = fakes.Display.Brightness[1] with { Supported = false };

        var result = TestHost.Run(fakes, "brightness", "+5");

        Assert.Equal("Brightness: 65%\n", result.Stdout);
        Assert.Equal((FakeDisplay.Display1, 65), Assert.Single(fakes.Display.BrightnessChanges));
    }

    [Fact]
    public void Brightness_Json_WithOneMonitor_IsANumber()
    {
        var fakes = new Fakes();
        fakes.Display.Brightness[1] = fakes.Display.Brightness[1] with { Supported = false };

        var result = TestHost.Run(fakes, "brightness", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal(60, doc.RootElement.GetProperty("brightness").GetInt32());
    }

    [Fact]
    public void Brightness_WithoutSupportingMonitor_Fails()
    {
        var fakes = new Fakes();
        fakes.Display.Brightness.Clear();

        var result = TestHost.Run(fakes, "brightness");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("DDC/CI", result.Stderr);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("101")]
    public void Brightness_RejectsBadValues(string arg)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "brightness", arg);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Empty(fakes.Display.BrightnessChanges);
    }

    [Theory]
    [InlineData("brightness", "+5", "monitor", "2")]
    [InlineData("brightness", "monitor", "2", "+5")]
    [InlineData("bright", "+5", "mon", "2")]
    public void Brightness_WithMonitor_TouchesOnlyThatMonitor(params string[] argv)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal((FakeDisplay.Display2, 45), Assert.Single(fakes.Display.BrightnessChanges));
        Assert.Equal("Brightness: 45%\n", result.Stdout);
    }

    [Fact]
    public void Brightness_WithMonitor_ShowsThatMonitorOnly()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "brightness", "monitor", "1");

        Assert.Empty(fakes.Display.BrightnessChanges);
        Assert.Equal("Brightness: 60%\n", result.Stdout);
    }

    [Theory]
    [InlineData("brightness", "monitor", "3")]
    [InlineData("brightness", "+5", "monitor")]
    [InlineData("brightness", "+5", "monitor", "x")]
    public void Brightness_WithBadMonitor_Fails(params string[] argv)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Empty(fakes.Display.BrightnessChanges);
    }

    [Fact]
    public void Brightness_MonitorWithoutDdc_Fails()
    {
        var fakes = new Fakes();
        fakes.Display.Brightness[1] = fakes.Display.Brightness[1] with { Supported = false };

        var result = TestHost.Run(fakes, "brightness", "monitor", "2");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Monitor 2 does not answer", result.Stderr);
    }

    [Theory]
    [InlineData("hdr", "monitor", "1")]
    [InlineData("hdr", "monitor", "main")]
    [InlineData("h", "mon", "primary")]
    [InlineData("hdr", "on", "monitor", "1")]
    [InlineData("hdr", "monitor", "1", "on")]
    public void Hdr_WithMonitor_TouchesOnlyThatMonitor(params string[] argv)
    {
        var fakes = new Fakes();
        fakes.Display.Hdr[1] = new HdrDisplay(FakeDisplay.Display2, Supported: true, Enabled: false);

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal((FakeDisplay.Display1, true), Assert.Single(fakes.Display.HdrChanges));
        Assert.Equal("HDR: on\n", result.Stdout);
        Assert.False(fakes.Display.Hdr[1].Enabled);
    }

    [Fact]
    public void Hdr_WithMonitor_TogglesThatMonitorRegardlessOfTheOthers()
    {
        var fakes = new Fakes();
        fakes.Display.Hdr[0] = fakes.Display.Hdr[0] with { Enabled = true };
        fakes.Display.Hdr[1] = new HdrDisplay(FakeDisplay.Display2, Supported: true, Enabled: false);

        var result = TestHost.Run(fakes, "hdr", "monitor", "1");

        Assert.Equal((FakeDisplay.Display1, false), Assert.Single(fakes.Display.HdrChanges));
        Assert.Equal("HDR: off\n", result.Stdout);
    }

    [Fact]
    public void Hdr_WithMonitorThatHasNoHdr_Fails()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "hdr", "monitor", "2");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Monitor 2 does not support HDR", result.Stderr);
        Assert.Empty(fakes.Display.HdrChanges);
    }

    [Theory]
    [InlineData("hdr", "monitor", "3")]
    [InlineData("hdr", "monitor")]
    [InlineData("hdr", "monitor", "x")]
    [InlineData("hdr", "on", "off", "monitor", "1")]
    public void Hdr_WithBadMonitorOption_Fails(params string[] argv)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Empty(fakes.Display.HdrChanges);
    }

    [Fact]
    public void Brightness_WithMonitorMain_UsesThePrimaryMonitor()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "brightness", "+5", "monitor", "main");

        Assert.Equal((FakeDisplay.Display1, 65), Assert.Single(fakes.Display.BrightnessChanges));
        Assert.Equal("Brightness: 65%\n", result.Stdout);
    }

    [Fact]
    public void Brightness_WhenOneMonitorFails_StillSetsTheOthersAndReportsBoth()
    {
        var fakes = new Fakes();
        fakes.Display.FailingDevices.Add(FakeDisplay.Display1);

        var result = TestHost.Run(fakes, "brightness", "50");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal((FakeDisplay.Display2, 50), Assert.Single(fakes.Display.BrightnessChanges));
        Assert.Contains("DISPLAY1 does not answer", result.Stderr);
        Assert.Contains("50%", result.Stdout);
        Assert.Contains("60%", result.Stdout);
    }
}

using System.Text.Json;

namespace Wctl.Tests.Commands;

public class AudioCommandsTests
{
    [Theory]
    [InlineData("vol")]
    [InlineData("volume")]
    [InlineData("v")]
    [InlineData("vol", "status")]
    public void Vol_ShowsTheVolume(params string[] argv)
    {
        var result = TestHost.Run(argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal("Volume: 47%\n", result.Stdout);
    }

    [Fact]
    public void Vol_WhenMuted_SaysSo()
    {
        var fakes = new Fakes { Audio = { Muted = true } };

        var result = TestHost.Run(fakes, "vol");

        Assert.Equal("Volume: 47% (muted)\n", result.Stdout);
    }

    [Theory]
    [InlineData("+5", 52)]
    [InlineData("-5", 42)]
    [InlineData("30", 30)]
    [InlineData("0", 0)]
    [InlineData("100", 100)]
    [InlineData("+60", 100)]
    [InlineData("-60", 0)]
    public void Vol_SetsTheVolume(string arg, int expected)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "vol", arg);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(expected, fakes.Audio.Volume);
        Assert.Equal($"Volume: {expected}%\n", result.Stdout);
    }

    [Fact]
    public void Vol_SettingAVolume_Unmutes()
    {
        var fakes = new Fakes { Audio = { Muted = true } };

        TestHost.Run(fakes, "vol", "+5");

        Assert.False(fakes.Audio.Muted);
        Assert.Equal(52, fakes.Audio.Volume);
    }

    [Theory]
    [InlineData("101")]
    [InlineData("abc")]
    [InlineData("+")]
    [InlineData("5%")]
    public void Vol_RejectsBadValues(string arg)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "vol", arg);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("error:", result.Stderr);
        Assert.Equal(47, fakes.Audio.Volume);
    }

    [Fact]
    public void Vol_RejectsExtraArguments()
    {
        var result = TestHost.Run("vol", "5", "6");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Too many arguments for 'vol'", result.Stderr);
    }

    [Fact]
    public void Vol_MuteAndUnmute_AreExplicit()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "vol", "mute");
        Assert.True(fakes.Audio.Muted);

        TestHost.Run(fakes, "vol", "mute");
        Assert.True(fakes.Audio.Muted);

        var result = TestHost.Run(fakes, "vol", "unmute");
        Assert.False(fakes.Audio.Muted);
        Assert.Equal("Volume: 47%\n", result.Stdout);
    }

    [Fact]
    public void Vol_Json_HasTypedVolumeAndMuted()
    {
        var fakes = new Fakes { Audio = { Muted = true } };

        var result = TestHost.Run(fakes, "vol", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal(47, doc.RootElement.GetProperty("volume").GetInt32());
        Assert.True(doc.RootElement.GetProperty("muted").GetBoolean());
    }

    [Fact]
    public void Mute_Toggles()
    {
        var fakes = new Fakes();

        var first = TestHost.Run(fakes, "mute");
        Assert.True(fakes.Audio.Muted);
        Assert.Equal("Volume: 47% (muted)\n", first.Stdout);

        var second = TestHost.Run(fakes, "mute");
        Assert.False(fakes.Audio.Muted);
        Assert.Equal("Volume: 47%\n", second.Stdout);
    }

    [Theory]
    [InlineData("on", true)]
    [InlineData("off", false)]
    public void Mute_OnAndOff_AreExplicit(string arg, bool expected)
    {
        var fakes = new Fakes { Audio = { Muted = !expected } };

        TestHost.Run(fakes, "mute", arg);
        TestHost.Run(fakes, "mute", arg);

        Assert.Equal(expected, fakes.Audio.Muted);
    }

    [Fact]
    public void Mute_Status_ChangesNothing()
    {
        var fakes = new Fakes { Audio = { Muted = true } };

        var result = TestHost.Run(fakes, "mute", "status");

        Assert.True(fakes.Audio.Muted);
        Assert.Equal("Volume: 47% (muted)\n", result.Stdout);
    }

    [Fact]
    public void Mute_RejectsUnknownWords()
    {
        var result = TestHost.Run("mute", "please");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Got 'please'", result.Stderr);
    }

    [Fact]
    public void Mic_Toggles()
    {
        var fakes = new Fakes();

        var first = TestHost.Run(fakes, "mic");
        Assert.True(fakes.Audio.MicMuted);
        Assert.Equal("Mic: muted\n", first.Stdout);

        var second = TestHost.Run(fakes, "mic");
        Assert.False(fakes.Audio.MicMuted);
        Assert.Equal("Mic: unmuted\n", second.Stdout);
    }

    [Theory]
    [InlineData("mute", true)]
    [InlineData("unmute", false)]
    [InlineData("on", true)]
    [InlineData("off", false)]
    public void Mic_MuteAndUnmute_AreExplicit(string arg, bool expected)
    {
        var fakes = new Fakes { Audio = { MicMuted = !expected } };

        TestHost.Run(fakes, "mic", arg);
        TestHost.Run(fakes, "mic", arg);

        Assert.Equal(expected, fakes.Audio.MicMuted);
    }

    [Fact]
    public void Mic_Json_IsAString()
    {
        var result = TestHost.Run("mic", "status", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal("unmuted", doc.RootElement.GetProperty("mic").GetString());
    }

    [Fact]
    public void Audio_ListsOutputsAndMarksTheCurrentOne()
    {
        var result = TestHost.Run("audio");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var lines = result.Stdout.Split('\n');
        Assert.Contains("●", Assert.Single(lines, l => l.Contains("FiiO", StringComparison.Ordinal)));
        Assert.DoesNotContain("●", Assert.Single(lines, l => l.Contains("Focusrite", StringComparison.Ordinal)));
        Assert.DoesNotContain("●", Assert.Single(lines, l => l.Contains("BTD", StringComparison.Ordinal)));
    }

    [Fact]
    public void Audio_Json_ListsOutputsWithCurrentFlag()
    {
        var result = TestHost.Run("audio", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        var outputs = doc.RootElement.GetProperty("outputs").EnumerateArray().ToList();
        Assert.Equal(3, outputs.Count);
        Assert.Equal("1", outputs[0].GetProperty("index").GetString());
        Assert.Equal("Speakers (FiiO K11)", outputs[0].GetProperty("name").GetString());
        Assert.True(outputs[0].GetProperty("current").GetBoolean());
        Assert.False(outputs[1].GetProperty("current").GetBoolean());
    }

    [Theory]
    [InlineData("btd")]
    [InlineData("BTD 600")]
    [InlineData("headphones")]
    [InlineData("3")]
    public void Audio_SwitchesByNameOrNumber(string query)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "audio", query);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal("id-btd", fakes.Audio.DefaultOutputId);
        Assert.Equal("Audio output: Headphones (BTD 600)\n", result.Stdout);
    }

    [Fact]
    public void Audio_JoinsWordsInTheQuery()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "audio", "focusrite", "usb");

        Assert.Equal("id-focusrite", fakes.Audio.DefaultOutputId);
    }

    [Fact]
    public void Audio_AmbiguousName_FailsAndListsMatches()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "audio", "speakers");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("More than one audio output matches 'speakers'", result.Stderr);
        Assert.Contains("FiiO", result.Stderr);
        Assert.Contains("Focusrite", result.Stderr);
        Assert.Equal("id-fiio", fakes.Audio.DefaultOutputId);
    }

    [Theory]
    [InlineData("airpods")]
    [InlineData("0")]
    [InlineData("4")]
    public void Audio_NoMatch_FailsAndListsAvailable(string query)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "audio", query);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("audio output", result.Stderr);
        Assert.Equal("id-fiio", fakes.Audio.DefaultOutputId);
    }

    [Fact]
    public void Audio_WithoutDevices_Fails()
    {
        var fakes = new Fakes();
        fakes.Audio.Outputs.Clear();

        var result = TestHost.Run(fakes, "audio");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("No audio output devices found", result.Stderr);
    }
}

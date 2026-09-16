using System.Text.Json;

namespace Wctl.Tests.Commands;

public class MediaCommandsTests
{
    [Theory]
    [InlineData("play", "play/pause", "play or pause")]
    [InlineData("pause", "play/pause", "play or pause")]
    [InlineData("pp", "play/pause", "play or pause")]
    [InlineData("next", "next", "next track")]
    [InlineData("skip", "next", "next track")]
    [InlineData("prev", "previous", "previous track")]
    [InlineData("previous", "previous", "previous track")]
    [InlineData("back", "previous", "previous track")]
    [InlineData("stop", "stop", "stop")]
    public void EachCommandSendsItsKeyOnce(string command, string key, string reported)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(key, Assert.Single(fakes.Media.Sent));
        Assert.Equal($"Media: {reported}\n", result.Stdout);
    }

    [Theory]
    [InlineData("play")]
    [InlineData("next")]
    [InlineData("prev")]
    [InlineData("stop")]
    public void TheyTakeNoArguments(string command)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command, "spotify");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains($"Too many arguments for '{command}'", result.Stderr);
        Assert.Empty(fakes.Media.Sent);
    }

    [Fact]
    public void PlayTwice_TogglesTwice()
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "play");
        TestHost.Run(fakes, "pause");

        Assert.Equal(["play/pause", "play/pause"], fakes.Media.Sent);
    }

    [Fact]
    public void Json_SaysWhatWasSent()
    {
        var result = TestHost.Run("next", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal("next track", doc.RootElement.GetProperty("media").GetString());
    }

    [Fact]
    public void TheyShowUpInHelpUnderMedia()
    {
        var result = TestHost.Run("help");

        Assert.Contains("Media", result.Stdout);
        Assert.Contains("play", result.Stdout);
        Assert.Contains("Skip to the next track", result.Stdout);
    }

    [Fact]
    public void AScene_CanUseThem()
    {
        var fakes = new Fakes();
        fakes.Scenes.Text = "[deepwork]\nvol 20\npause\n";

        var result = TestHost.Run(fakes, "deepwork");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal("play/pause", Assert.Single(fakes.Media.Sent));
        Assert.Contains("✓ pause", result.Stdout);
    }
}

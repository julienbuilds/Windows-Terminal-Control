using Wctl.Scenes;

namespace Wctl.Tests.Commands;

public class SceneEditorTests
{
    private static readonly string[] CapturedFromFakes =
    [
        "vol 47",
        "mic unmute",
        "audio \"Speakers (FiiO K11)\"",
        "hdr off",
        "brightness 60 monitor 1",
        "brightness 40 monitor 2",
        "open firefox",
        "open Discord",
        "open Terminal",
        "open Spotify",
        "wait 2s",
        "move firefox 100 100 1200 800",
        "move Discord 2",
        "max Discord",
        "move WindowsTerminal 1500 200 1000 600",
        "top WindowsTerminal on",
        "min Spotify",
    ];

    private static IReadOnlyList<string> SavedSteps(Fakes fakes, string name)
        => SceneFile.Parse(Assert.Single(fakes.Scenes.Written)).Single(s => s.Name == name).Steps;

    [Fact]
    public void New_CapturesTheCurrentSetup_AndSavesOnS()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("s");

        var result = TestHost.Run(fakes, "scene", "new", "work");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(CapturedFromFakes, SavedSteps(fakes, "work"));
        Assert.Contains("Scene work:", result.Stdout);
        Assert.Contains("  1  vol 47", result.Stdout);
        Assert.EndsWith("Saved: work (17 steps)\n", result.Stdout);
    }

    [Fact]
    public void New_CapturesMuteAndMicMute()
    {
        var fakes = new Fakes { Audio = { Muted = true, MicMuted = true } };
        fakes.Prompt.Answer("s");

        TestHost.Run(fakes, "scene", "new", "work");

        var steps = SavedSteps(fakes, "work");
        Assert.Equal(["vol 47", "mute on", "mic mute"], steps.Take(3));
    }

    [Fact]
    public void New_WithSeveralHdrDisplays_CapturesEachMonitor()
    {
        var fakes = new Fakes();
        fakes.Display.Hdr[0] = fakes.Display.Hdr[0] with { Enabled = true };
        fakes.Display.Hdr[1] = new(FakeDisplay.Display2, Supported: true, Enabled: false);
        fakes.Prompt.Answer("s");

        TestHost.Run(fakes, "scene", "new", "work");

        var steps = SavedSteps(fakes, "work");
        Assert.Contains("hdr on monitor 1", steps);
        Assert.Contains("hdr off monitor 2", steps);
        Assert.DoesNotContain("hdr on", steps);
    }

    [Fact]
    public void New_Empty_StartsWithoutSteps()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("a vol 20", "s");

        var result = TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains("Scene work: no steps yet.", result.Stdout);
        Assert.Equal(["vol 20"], SavedSteps(fakes, "work"));
    }

    [Fact]
    public void Editor_RemoveMoveAddAndTest()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("a vol 20", "a hdr off", "a mute on", "r 2", "m 2 1", "t 1", "s");

        var result = TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Equal(["mute on", "vol 20"], SavedSteps(fakes, "work"));
        Assert.Contains("✓ mute on", result.Stdout);
        Assert.True(fakes.Audio.Muted);
    }

    [Fact]
    public void Editor_Quit_DoesNotSave()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("a vol 20", "q");

        var result = TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Empty(fakes.Scenes.Written);
        Assert.EndsWith("Not saved.\n", result.Stdout);
    }

    [Fact]
    public void Editor_EndOfInput_DoesNotSaveAndFails()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Empty(fakes.Scenes.Written);
        Assert.Contains("Input ended. Not saved.", result.Stdout);
    }

    [Fact]
    public void Editor_BadStepNumber_ExplainsAndContinues()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("r 99", "r x", "m 1", "s");

        var result = TestHost.Run(fakes, "scene", "new", "work");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains("Which step? A number from 1 to 17.", result.Stdout);
        Assert.Equal(17, SavedSteps(fakes, "work").Count);
    }

    [Fact]
    public void Editor_UnknownCommand_ShowsTheCommands()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("x", "?", "s");

        var result = TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Contains("Unknown editor command 'x'", result.Stdout);
        Assert.Contains("r <n> remove", result.Stdout);
    }

    [Fact]
    public void Editor_AddUnknownWord_WarnsButAccepts()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("a blender", "s");

        var result = TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Contains("'blender' is not a command, so this step will open an app called blender.", result.Stdout);
        Assert.Equal(["blender"], SavedSteps(fakes, "work"));
    }

    [Fact]
    public void Editor_AddSceneStep_IsRejected()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("a scene other", "a s other", "a", "s");

        var result = TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Contains("A scene cannot run another scene.", result.Stdout);
        Assert.Contains("Add what?", result.Stdout);
        Assert.Empty(SavedSteps(fakes, "work"));
    }

    [Fact]
    public void Editor_AddKeepsQuotes()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("a audio \"BTD 600\"", "s");

        TestHost.Run(fakes, "scene", "new", "work", "empty");

        Assert.Equal(["audio \"BTD 600\""], SavedSteps(fakes, "work"));
    }

    [Fact]
    public void Edit_LoadsTheExistingSteps_AndReplacesTheScene()
    {
        var fakes = new Fakes();
        fakes.Scenes.Text = "[work]\nvol 15\n\n[other]\nhdr on\n";
        fakes.Prompt.Answer("a hdr off", "s");

        var result = TestHost.Run(fakes, "scene", "edit", "work");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var scenes = SceneFile.Parse(Assert.Single(fakes.Scenes.Written));
        Assert.Equal(["vol 15", "hdr off"], scenes.Single(s => s.Name == "work").Steps);
        Assert.Equal(["hdr on"], scenes.Single(s => s.Name == "other").Steps);
    }

    [Theory]
    [InlineData("scene", "edit", "nope")]
    [InlineData("scene", "edit")]
    [InlineData("scene", "new")]
    [InlineData("scene", "new", "my scene")]
    [InlineData("scene", "new", "ls")]
    [InlineData("scene", "new", "v")]
    [InlineData("scene", "new", "work", "full")]
    public void NewAndEdit_RejectBadNamesAndArguments(params string[] argv)
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("s");

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Empty(fakes.Scenes.Written);
    }

    [Fact]
    public void New_ExistingName_Fails()
    {
        var fakes = new Fakes();
        fakes.Scenes.Text = "[work]\nvol 15\n";

        var result = TestHost.Run(fakes, "scene", "new", "work");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("A scene named 'work' exists", result.Stderr);
    }

    [Fact]
    public void New_NameOfAnApp_WarnsAndContinues()
    {
        var fakes = new Fakes();
        fakes.Prompt.Answer("q");

        var result = TestHost.Run(fakes, "scene", "new", "steam", "empty");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains("'steam' is also the app Steam", result.Stdout);
    }

    [Fact]
    public void New_WithJson_IsRefused()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "scene", "new", "work", "--json");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("interactive", result.RawStdout);
        Assert.Empty(fakes.Scenes.Written);
    }
}

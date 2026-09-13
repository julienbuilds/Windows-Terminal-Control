using System.Text.Json;
using Wctl.Platform;

namespace Wctl.Tests.Commands;

public class SceneCommandsTests
{
    private const string File = """
        [work]
        hdr off
        vol 15
        audio btd

        [empty]
        """;

    private static Fakes WithScenes(string text = File)
    {
        var fakes = new Fakes();
        fakes.Scenes.Text = text;
        return fakes;
    }

    [Theory]
    [InlineData("scene", "work")]
    [InlineData("s", "work")]
    [InlineData("scene", "WORK")]
    [InlineData("work")]
    public void Scene_RunsEveryStepAndReportsEachOne(params string[] argv)
    {
        var fakes = WithScenes();
        fakes.Display.Hdr[0] = fakes.Display.Hdr[0] with { Enabled = true };

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.False(fakes.Display.Hdr[0].Enabled);
        Assert.Equal(15, fakes.Audio.Volume);
        Assert.Equal("id-btd", fakes.Audio.DefaultOutputId);
        var lines = result.Stdout.Split('\n');
        Assert.Equal("Scene: work", lines[0]);
        Assert.Contains("✓ hdr off", lines[1]);
        Assert.Contains("HDR: off", lines[1]);
        Assert.Contains("✓ vol 15", lines[2]);
        Assert.Contains("Volume: 15%", lines[2]);
        Assert.Contains("✓ audio btd", lines[3]);
        Assert.Contains("Audio output: Headphones (BTD 600)", lines[3]);
        Assert.Equal("3 steps, all done.", lines[4]);
    }

    [Fact]
    public void Scene_FailingStep_DoesNotStopTheRest()
    {
        var fakes = WithScenes("[gaming]\nclose blender\nvol 35\n");

        var result = TestHost.Run(fakes, "gaming");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(35, fakes.Audio.Volume);
        Assert.Contains("✗ close blender", result.Stdout);
        Assert.Contains("No window matches 'blender'", result.Stdout);
        Assert.Contains("✓ vol 35", result.Stdout);
        Assert.Contains("1 of 2 steps failed", result.Stderr);
    }

    [Fact]
    public void Scene_Json_ListsStepsWithOkFlags()
    {
        var fakes = WithScenes("[gaming]\nclose blender\nvol 35\n");

        var result = TestHost.Run(fakes, "gaming", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal("gaming", doc.RootElement.GetProperty("scene").GetString());
        var steps = doc.RootElement.GetProperty("steps").EnumerateArray().ToList();
        Assert.Equal(2, steps.Count);
        Assert.Equal("close blender", steps[0].GetProperty("step").GetString());
        Assert.False(steps[0].GetProperty("ok").GetBoolean());
        Assert.True(steps[1].GetProperty("ok").GetBoolean());
        Assert.Equal("Volume: 35%", steps[1].GetProperty("result").GetString());
        Assert.Equal(1, doc.RootElement.GetProperty("failed").GetInt32());
        Assert.Equal(2, doc.RootElement.GetProperty("step_count").GetInt32());
    }

    [Fact]
    public void Scene_WaitStep_AdvancesTime()
    {
        var fakes = WithScenes("[w]\nwait 2s\n");
        var start = fakes.Clock.Now;

        var result = TestHost.Run(fakes, "w");

        Assert.Equal(start + TimeSpan.FromSeconds(2), fakes.Clock.Now);
        Assert.Contains("✓ wait 2s", result.Stdout);
        Assert.Contains("Waited: 2s", result.Stdout);
    }

    [Fact]
    public void Scene_WindowStepAfterOpen_WaitsForTheWindow()
    {
        var fakes = WithScenes("[w]\nopen notepad\nmove notepad left\n");
        var notepad = new WindowInfo(42, "Untitled - Notepad", "Notepad", 4200, 1, false, false, false, false);
        fakes.Windows.Appearing.Add((3, notepad));
        var start = fakes.Clock.Now;

        var result = TestHost.Run(fakes, "w");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(FakeShell.NotepadId, Assert.Single(fakes.Shell.Opened).Target);
        Assert.Equal((42, new Rect(0, 0, 1536, 1680)), Assert.Single(fakes.Windows.SetFrames));
        Assert.Equal(start + TimeSpan.FromSeconds(1), fakes.Clock.Now);
        Assert.Contains("✓ move notepad left", result.Stdout);
    }

    [Fact]
    public void Scene_WindowStepAfterOpen_GivesUpAfterFiveSeconds()
    {
        var fakes = WithScenes("[w]\nopen notepad\nmove ghost left\n");
        var start = fakes.Clock.Now;

        var result = TestHost.Run(fakes, "w");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.True(fakes.Clock.Now >= start + TimeSpan.FromSeconds(5));
        Assert.Contains("✗ move ghost left", result.Stdout);
    }

    [Fact]
    public void Scene_WindowStepsAfterOpen_ShareOneTimeBudget()
    {
        var fakes = WithScenes("[w]\nopen notepad\nmove ghost left\ncenter ghost\nclose ghost\n");
        var start = fakes.Clock.Now;

        var result = TestHost.Run(fakes, "w");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("3 of 4 steps failed", result.Stderr);
        Assert.InRange(fakes.Clock.Now - start, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(6));
    }

    [Fact]
    public void Scene_WindowStepWithoutOpen_FailsAtOnce()
    {
        var fakes = WithScenes("[w]\nmove ghost left\n");
        var start = fakes.Clock.Now;

        var result = TestHost.Run(fakes, "w");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(start, fakes.Clock.Now);
    }

    [Fact]
    public void Scene_CannotRunAnotherScene()
    {
        var fakes = WithScenes("[outer]\nscene inner\ninner\n[inner]\nvol 10\n");

        var result = TestHost.Run(fakes, "outer");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(47, fakes.Audio.Volume);
        Assert.Contains("a scene cannot run another scene", result.Stdout);
        Assert.Contains("Nothing called 'inner' was found", result.Stdout);
    }

    [Fact]
    public void Scene_UnknownName_Fails()
    {
        var result = TestHost.Run(WithScenes(), "scene", "nope");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("No scene named 'nope'", result.Stderr);
    }

    [Fact]
    public void UnknownWord_StillOpensAppsWhenNoSceneMatches()
    {
        var fakes = WithScenes();

        var result = TestHost.Run(fakes, "spotify");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(FakeShell.SpotifyId, Assert.Single(fakes.Shell.Opened).Target);
    }

    [Fact]
    public void UnknownWord_WithBrokenScenesFile_StillOpensApps()
    {
        var fakes = WithScenes("vol 15\n");

        var open = TestHost.Run(fakes, "spotify");
        var list = TestHost.Run(fakes, "scene", "list");

        Assert.Equal(ExitCodes.Ok, open.ExitCode);
        Assert.Equal(ExitCodes.Failure, list.ExitCode);
        Assert.Contains("line 1", list.Stderr);
    }

    [Theory]
    [InlineData("scene")]
    [InlineData("scene", "list")]
    [InlineData("scenes")]
    public void SceneList_ShowsNamesAndStepCounts(params string[] argv)
    {
        var result = TestHost.Run(WithScenes(), argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        var lines = result.Stdout.Split('\n');
        Assert.Contains("3", Assert.Single(lines, l => l.Contains("work", StringComparison.Ordinal)));
        Assert.Contains("0", Assert.Single(lines, l => l.Contains("empty", StringComparison.Ordinal)));
    }

    [Fact]
    public void SceneList_WithoutFile_SaysHowToStart()
    {
        var result = TestHost.Run("scene", "list");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal("No scenes yet. Create one with 'w scene new <name>'.\n", result.Stdout);
    }

    [Fact]
    public void SceneShow_ListsTheSteps()
    {
        var result = TestHost.Run(WithScenes(), "scene", "show", "work");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.StartsWith("Scene: work\n", result.Stdout, StringComparison.Ordinal);
        Assert.Contains("hdr off", result.Stdout);
        Assert.Contains("audio btd", result.Stdout);
    }

    [Fact]
    public void SceneDelete_RewritesTheFileWithoutIt()
    {
        var fakes = WithScenes();

        var result = TestHost.Run(fakes, "scene", "delete", "work");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal("Deleted: work\n", result.Stdout);
        var written = Assert.Single(fakes.Scenes.Written);
        Assert.DoesNotContain("[work]", written);
        Assert.Contains("[empty]", written);
    }

    [Theory]
    [InlineData("scene", "show")]
    [InlineData("scene", "delete")]
    public void SceneShowAndDelete_NeedAName(params string[] argv)
    {
        var result = TestHost.Run(WithScenes(), argv);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Which scene?", result.Stderr);
    }

    [Fact]
    public void SceneFile_CreatesTheFileWhenMissingAndOpensIt()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "scene", "file");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.StartsWith("# wctl scenes", Assert.Single(fakes.Scenes.Written), StringComparison.Ordinal);
        Assert.Equal((fakes.Scenes.FilePath, null), Assert.Single(fakes.Shell.Opened));
        Assert.Equal($"Scenes file: {fakes.Scenes.FilePath}\n", result.Stdout);
    }

    [Fact]
    public void Wait_BlocksForTheDuration()
    {
        var fakes = new Fakes();
        var start = fakes.Clock.Now;

        var result = TestHost.Run(fakes, "wait", "90s");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(start + TimeSpan.FromSeconds(90), fakes.Clock.Now);
        Assert.EndsWith("Waited: 1m 30s\n", result.Stdout);
    }

    [Theory]
    [InlineData("wait")]
    [InlineData("wait", "soon")]
    public void Wait_NeedsAValidDuration(params string[] argv)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(0, fakes.Clock.Delays);
    }
}

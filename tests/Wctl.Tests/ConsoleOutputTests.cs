using Spectre.Console.Testing;
using Wctl.Cli;

namespace Wctl.Tests;

public class ConsoleOutputTests
{
    [Fact]
    public void State_PrintsLabelAndValue()
    {
        var (output, stdout, _) = Create();

        output.State("Volume", 47, "47%");
        output.State("HDR", true);
        output.State("Mic", false, "muted");

        Assert.Equal("Volume: 47%\nHDR: on\nMic: muted\n", TestHost.Normalize(stdout.Output));
    }

    [Fact]
    public void Details_AreNotShown()
    {
        var (output, stdout, _) = Create();

        output.State("Volume", 47, "47% (muted)");
        output.Detail("Muted", true);

        Assert.Equal("Volume: 47% (muted)\n", TestHost.Normalize(stdout.Output));
    }

    [Fact]
    public void Status_RewritesOneLineAndClears()
    {
        var (output, stdout, _) = Create();

        output.Status("10s left");
        output.Status("9s left");
        output.Status(string.Empty);
        output.State("Kept awake", "10s");

        var text = stdout.Output;
        Assert.Contains("\r10s left", text);
        Assert.Contains("\r9s left ", text);
        Assert.DoesNotContain("\n10s", text);
        Assert.EndsWith("Kept awake: 10s\n", TestHost.Normalize(text));
    }

    [Fact]
    public void Step_PrintsMarkStepAndResult()
    {
        var (output, stdout, _) = Create();

        output.StepResult("vol 15", true, "Volume: 15%");
        output.StepResult("close blender", false, "No window matches 'blender'.");

        var lines = TestHost.Normalize(stdout.Output).Split('\n');
        Assert.StartsWith("  ✓ vol 15", lines[0], StringComparison.Ordinal);
        Assert.EndsWith("Volume: 15%", lines[0], StringComparison.Ordinal);
        Assert.StartsWith("  ✗ close blender", lines[1], StringComparison.Ordinal);
        Assert.EndsWith("No window matches 'blender'.", lines[1], StringComparison.Ordinal);
    }

    [Fact]
    public void Action_PrintsTargetAndResult()
    {
        var (output, stdout, _) = Create();

        output.Action("firefox", "left half of monitor 1");

        Assert.Equal("firefox: left half of monitor 1\n", TestHost.Normalize(stdout.Output));
    }

    [Fact]
    public void Table_WithoutRows_PrintsNothing()
    {
        var (output, stdout, _) = Create();

        output.Table("windows", ["index"], []);

        Assert.Equal(string.Empty, stdout.Output);
    }

    [Fact]
    public void Table_MarksTheCurrentRow()
    {
        var (output, stdout, _) = Create();

        output.Table("devices", ["device"], [["Speakers"], ["Headphones"]], new TableOptions(CurrentRow: 1));

        var lines = TestHost.Normalize(stdout.Output).Split('\n');
        Assert.DoesNotContain("●", Assert.Single(lines, l => l.Contains("Speakers", StringComparison.Ordinal)));
        Assert.Contains("●", Assert.Single(lines, l => l.Contains("Headphones", StringComparison.Ordinal)));
    }

    [Fact]
    public void Table_WithTitleAndNoHeader_PrintsTitleOnly()
    {
        var (output, stdout, _) = Create();

        output.Table("audio", ["command", "description"], [["vol", "Volume"]], new TableOptions(Title: "Audio", Header: false));

        var text = stdout.Output;
        Assert.Contains("Audio", text);
        Assert.Contains("vol", text);
        Assert.DoesNotContain("description", text);
    }

    [Fact]
    public void Error_GoesToStderr()
    {
        var (output, stdout, stderr) = Create();

        output.Fail("boom");

        Assert.Equal(string.Empty, stdout.Output);
        Assert.Contains("error: boom", stderr.Output);
    }

    private static (ConsoleOutput Output, TestConsole Stdout, TestConsole Stderr) Create()
    {
        var stdout = TestHost.Console();
        var stderr = TestHost.Console();
        return (new ConsoleOutput(stdout, stderr), stdout, stderr);
    }
}

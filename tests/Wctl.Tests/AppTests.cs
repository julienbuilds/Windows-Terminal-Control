using System.Text.Json;
using Wctl.Cli;

namespace Wctl.Tests;

public class AppTests
{
    [Fact]
    public void NoArguments_ShowsCommandOverview()
    {
        var result = TestHost.Run();

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains(Help.Tagline, result.Stdout);
        Assert.Contains("help", result.Stdout);
        Assert.Contains("version", result.Stdout);
    }

    [Fact]
    public void UnknownCommand_FailsWithMessage()
    {
        var result = TestHost.Run("frobnicate");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Unknown command 'frobnicate'", result.Stderr);
        Assert.Equal(string.Empty, result.Stdout);
    }

    [Theory]
    [InlineData("version")]
    [InlineData("--version")]
    [InlineData("-V")]
    public void Version_PrintsTheVersion(string arg)
    {
        var result = TestHost.Run(arg);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains($"wctl: {VersionInfo.Current}", result.Stdout);
    }

    [Theory]
    [InlineData("help", "version")]
    [InlineData("version", "-h")]
    [InlineData("version", "--help")]
    public void HelpForOneCommand_ShowsUsage(params string[] argv)
    {
        var result = TestHost.Run(argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains("Usage: w version", result.Stdout);
    }

    [Fact]
    public void HelpForUnknownCommand_Fails()
    {
        var result = TestHost.Run("help", "nope");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Unknown command 'nope'", result.Stderr);
    }

    [Fact]
    public void UnknownOption_Fails()
    {
        var result = TestHost.Run("version", "--loud");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("--loud", result.Stderr);
    }

    [Fact]
    public void UnexpectedException_IsReportedWithExitCodeTwo()
    {
        var result = TestHost.RunWith(Throwing(new InvalidOperationException("boom")), "boom");

        Assert.Equal(ExitCodes.Unexpected, result.ExitCode);
        Assert.Contains("InvalidOperationException: boom", result.Stderr);
        Assert.Contains("--debug", result.Stderr);
    }

    [Fact]
    public void UnexpectedException_WithDebugFlag_Propagates()
    {
        var table = TestHost.TableWith(Throwing(new InvalidOperationException("boom")));

        Assert.Throws<InvalidOperationException>(() => TestHost.Run(table, "boom", "--debug"));
    }

    [Fact]
    public void CommandFailure_UsesItsMessageAndExitCode()
    {
        var result = TestHost.RunWith(Throwing(new WctlException("nothing to do")), "boom");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("error: nothing to do", result.Stderr);
    }

    [Fact]
    public void JsonFlag_WritesOneJsonObjectAndNothingElse()
    {
        var result = TestHost.Run("version", "--json");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(string.Empty, result.Stdout);
        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal(VersionInfo.Current, doc.RootElement.GetProperty("wctl").GetString());
    }

    [Fact]
    public void JsonFlag_PutsErrorsInTheObject()
    {
        var result = TestHost.Run("help", "nope", "--json");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Contains("nope", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public void HelpMarkdown_PrintsTheCommandsDocument()
    {
        var result = TestHost.Run("help", "--markdown");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.StartsWith("# Commands", result.RawStdout, StringComparison.Ordinal);
        Assert.Contains("| `w version` |", result.RawStdout);
    }

    private static CommandSpec Throwing(Exception e) => new()
    {
        Name = "boom",
        Group = Groups.Help,
        Summary = "Throws",
        Usage = "boom",
        Run = _ => throw e,
    };
}

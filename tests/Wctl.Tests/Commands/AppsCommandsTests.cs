using System.Text.Json;

namespace Wctl.Tests.Commands;

public class AppsCommandsTests
{
    [Theory]
    [InlineData("open", "spotify")]
    [InlineData("o", "spotify")]
    [InlineData("spotify")]
    [InlineData("SPOTIFY")]
    public void Open_LaunchesAnAppByName(params string[] argv)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, argv);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal((FakeShell.SpotifyId, null), Assert.Single(fakes.Shell.Opened));
        Assert.Equal("Spotify: opened\n", result.Stdout);
    }

    [Fact]
    public void Open_PassesExtraWordsToTheApp()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", "code", ".", "my folder", "--wait");

        Assert.Equal((FakeShell.CodeId, ". \"my folder\" --wait"), Assert.Single(fakes.Shell.Opened));
        Assert.Equal("Visual Studio Code: opened with . \"my folder\" --wait\n", result.Stdout);
    }

    [Theory]
    [InlineData("code")]
    [InlineData("Code.exe")]
    [InlineData("visual studio code")]
    public void Open_MatchesByExeNameAndFullName(string query)
    {
        var fakes = new Fakes();

        TestHost.Run(fakes, "open", query);

        Assert.Equal(FakeShell.CodeId, Assert.Single(fakes.Shell.Opened).Target);
    }

    [Fact]
    public void Open_AmbiguousName_FailsAndListsCandidates()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", "visual");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("more than one app: Visual Studio Code, Visual Studio 2022", result.Stderr);
        Assert.Empty(fakes.Shell.Opened);
    }

    [Fact]
    public void Open_UnknownName_Fails()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", "blender");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Nothing called 'blender' was found", result.Stderr);
        Assert.Contains("w apps blender", result.Stderr);
        Assert.Empty(fakes.Shell.Opened);
    }

    [Theory]
    [InlineData(".", @"C:\Users\Julien\CursorProjects")]
    [InlineData("..", @"C:\Users\Julien")]
    [InlineData("src", @"C:\Users\Julien\CursorProjects\src")]
    [InlineData(@".\src", @"C:\Users\Julien\CursorProjects\src")]
    [InlineData("~", @"C:\Users\Julien")]
    [InlineData("~/Downloads", @"C:\Users\Julien\Downloads")]
    [InlineData(@"C:\dev", @"C:\dev")]
    [InlineData("C:/dev", @"C:\dev")]
    public void Open_Folder_OpensIt(string input, string expected)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", input);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal((expected, null), Assert.Single(fakes.Shell.Opened));
        Assert.Equal($"Folder: {expected}\n", result.Stdout);
    }

    [Fact]
    public void Open_File_OpensIt()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", "notes.txt");

        Assert.Equal((@"C:\Users\Julien\CursorProjects\notes.txt", null), Assert.Single(fakes.Shell.Opened));
        Assert.Equal(@"File: C:\Users\Julien\CursorProjects\notes.txt" + "\n", result.Stdout);
    }

    [Fact]
    public void Open_PathThatDoesNotExist_Fails()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", @"src\nothing.txt");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains(@"There is no file or folder at 'C:\Users\Julien\CursorProjects\src\nothing.txt'", result.Stderr);
    }

    [Theory]
    [InlineData("https://example.com/a?b=1", "https://example.com/a?b=1")]
    [InlineData("http://localhost:3000", "http://localhost:3000")]
    [InlineData("www.example.com", "https://www.example.com")]
    [InlineData("mailto:someone@example.com", "mailto:someone@example.com")]
    [InlineData("steam://run/440", "steam://run/440")]
    public void Open_Link_OpensIt(string input, string expected)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", input);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal((expected, null), Assert.Single(fakes.Shell.Opened));
        Assert.Equal($"Link: {expected}\n", result.Stdout);
    }

    [Fact]
    public void Open_Settings_OpensWindowsSettings()
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, "open", "settings");

        Assert.Equal(("ms-settings:", null), Assert.Single(fakes.Shell.Opened));
        Assert.Equal("Settings: opened\n", result.Stdout);
    }

    [Fact]
    public void Open_WithoutArguments_Fails()
    {
        var result = TestHost.Run("open");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("What should be opened? Usage: w open", result.Stderr);
    }

    [Fact]
    public void Open_Json_HasTargetAndResult()
    {
        var result = TestHost.Run("spotify", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        Assert.Equal("Spotify", doc.RootElement.GetProperty("target").GetString());
        Assert.Equal("opened", doc.RootElement.GetProperty("result").GetString());
    }

    [Fact]
    public void Apps_ListsAllSortedByName()
    {
        var result = TestHost.Run("apps", "--json");

        using var doc = JsonDocument.Parse(result.RawStdout);
        var names = doc.RootElement.GetProperty("apps").EnumerateArray().Select(a => a.GetProperty("name").GetString()).ToList();
        Assert.Equal(["Firefox", "Notepad", "Spotify", "Steam", "Terminal", "Visual Studio 2022", "Visual Studio Code"], names);
    }

    [Fact]
    public void Apps_FiltersByNameOrExe()
    {
        var byName = TestHost.Run("apps", "visual", "studio");
        var byExe = TestHost.Run("apps", "devenv");

        Assert.Contains("Visual Studio Code", byName.Stdout);
        Assert.Contains("Visual Studio 2022", byName.Stdout);
        Assert.DoesNotContain("Spotify", byName.Stdout);
        Assert.Contains("Visual Studio 2022", byExe.Stdout);
        Assert.DoesNotContain("Visual Studio Code", byExe.Stdout);
    }

    [Fact]
    public void Apps_NoMatch_SaysSo()
    {
        var result = TestHost.Run("apps", "blender");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal("No app matches 'blender'.\n", result.Stdout);
    }

    [Theory]
    [InlineData(new string[0], @"C:\Users\Julien\CursorProjects")]
    [InlineData(new[] { ".." }, @"C:\Users\Julien")]
    [InlineData(new[] { "~/Downloads" }, @"C:\Users\Julien\Downloads")]
    [InlineData(new[] { @"C:\dev" }, @"C:\dev")]
    public void Folder_OpensTheFolder(string[] argv, string expected)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, ["folder", .. argv]);

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal((expected, null), Assert.Single(fakes.Shell.Opened));
        Assert.Equal($"Folder: {expected}\n", result.Stdout);
    }

    [Fact]
    public void Folder_Missing_Fails()
    {
        var result = TestHost.Run("folder", "nope");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains(@"There is no folder at 'C:\Users\Julien\CursorProjects\nope'", result.Stderr);
    }

    [Fact]
    public void Folder_GivenAFile_Fails()
    {
        var result = TestHost.Run("folder", "notes.txt");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("is a file, not a folder", result.Stderr);
    }

    [Theory]
    [InlineData("reveal")]
    [InlineData("select")]
    public void Reveal_SelectsTheFileInExplorer(string command)
    {
        var fakes = new Fakes();

        var result = TestHost.Run(fakes, command, "notes.txt");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Equal(@"C:\Users\Julien\CursorProjects\notes.txt", Assert.Single(fakes.Shell.Revealed));
        Assert.Equal(@"Selected: C:\Users\Julien\CursorProjects\notes.txt" + "\n", result.Stdout);
    }

    [Fact]
    public void Reveal_Missing_Fails()
    {
        var result = TestHost.Run("reveal", "nope.txt");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("There is no file or folder at", result.Stderr);
    }

    [Fact]
    public void Reveal_WithoutArguments_Fails()
    {
        var result = TestHost.Run("reveal");

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Contains("Which file? Usage: w reveal", result.Stderr);
    }
}

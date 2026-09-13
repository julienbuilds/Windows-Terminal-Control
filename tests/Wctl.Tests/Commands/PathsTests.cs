using Wctl.Commands.Apps;

namespace Wctl.Tests.Commands;

public class PathsTests
{
    private const string Cwd = @"C:\Users\Julien\CursorProjects";
    private const string Home = @"C:\Users\Julien";

    [Theory]
    [InlineData(".", Cwd)]
    [InlineData("..", @"C:\Users\Julien")]
    [InlineData("src", @"C:\Users\Julien\CursorProjects\src")]
    [InlineData(@"src\..\src\", @"C:\Users\Julien\CursorProjects\src\")]
    [InlineData("~", Home)]
    [InlineData("~/Downloads", @"C:\Users\Julien\Downloads")]
    [InlineData(@"~\Downloads", @"C:\Users\Julien\Downloads")]
    [InlineData(@"C:\dev", @"C:\dev")]
    [InlineData("D:/games/", @"D:\games\")]
    [InlineData("\"C:\\Program Files\"", @"C:\Program Files")]
    public void Resolve_MakesPathsAbsolute(string input, string expected)
    {
        Assert.Equal(expected, Paths.Resolve(input, Cwd, Home));
    }

    [Theory]
    [InlineData(".", true)]
    [InlineData("..", true)]
    [InlineData("~", true)]
    [InlineData("~/x", true)]
    [InlineData(@"src\x", true)]
    [InlineData("src/x", true)]
    [InlineData(@"C:\x", true)]
    [InlineData("spotify", false)]
    [InlineData("notes.txt", false)]
    [InlineData("visual studio code", false)]
    public void LooksLikePath_RecognizesPathSyntax(string input, bool expected)
    {
        Assert.Equal(expected, Paths.LooksLikePath(input));
    }

    [Theory]
    [InlineData("https://example.com", true, "https://example.com")]
    [InlineData("www.example.com/x", true, "https://www.example.com/x")]
    [InlineData("mailto:a@b.c", true, "mailto:a@b.c")]
    [InlineData("ms-settings:display", true, "ms-settings:display")]
    [InlineData(@"C:\dev", false, @"C:\dev")]
    [InlineData(@"\\server\share", false, @"\\server\share")]
    [InlineData("spotify", false, "spotify")]
    [InlineData("notes.txt", false, "notes.txt")]
    public void Urls_RecognizeLinks(string input, bool expected, string normalized)
    {
        Assert.Equal(expected, Urls.TryNormalize(input, out var url));
        Assert.Equal(normalized, url);
    }
}

using Wctl.Commands.Scenes;
using Wctl.Platform;

namespace Wctl.Tests.Commands;

public class AppNamesTests
{
    private static readonly FakeShell Shell = new();

    [Theory]
    [InlineData(@"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.2911.0_x64__8wekyb3d8bbwe\WindowsTerminal.exe", "Microsoft.WindowsTerminal_8wekyb3d8bbwe")]
    [InlineData(@"C:\Program Files\WindowsApps\Microsoft.WindowsNotepad_11.2409.9.0_x64__8wekyb3d8bbwe\Notepad\Notepad.exe", "Microsoft.WindowsNotepad_8wekyb3d8bbwe")]
    public void PackageFamily_ComesFromTheWindowsAppsFolderName(string path, string expected)
    {
        Assert.Equal(expected, AppNames.PackageFamily(path));
    }

    [Theory]
    [InlineData(@"C:\Program Files\Mozilla Firefox\firefox.exe")]
    [InlineData(@"C:\Program Files\WindowsApps\")]
    [InlineData(@"C:\Program Files\WindowsApps\NoUnderscores\x.exe")]
    [InlineData("")]
    public void PackageFamily_IsNullForEverythingElse(string path)
    {
        Assert.Null(AppNames.PackageFamily(path));
    }

    [Fact]
    public void ForOpen_KeepsTheProcessNameWhenAnAppMatchesIt()
    {
        var window = Window("firefox", @"C:\Program Files\Mozilla Firefox\firefox.exe");

        Assert.Equal("firefox", AppNames.ForOpen(window, Shell.Apps));
    }

    [Fact]
    public void ForOpen_UsesTheStoreAppNameForPackagedApps()
    {
        var window = Window("WindowsTerminal", @"C:\Program Files\WindowsApps\Microsoft.WindowsTerminal_1.21.2911.0_x64__8wekyb3d8bbwe\WindowsTerminal.exe");

        Assert.Equal("Terminal", AppNames.ForOpen(window, Shell.Apps));
    }

    [Fact]
    public void ForOpen_FallsBackToTheProcessName()
    {
        var window = Window("Discord", @"C:\Users\Julien\AppData\Local\Discord\app-1.0\Discord.exe");

        Assert.Equal("Discord", AppNames.ForOpen(window, Shell.Apps));
    }

    private static WindowInfo Window(string app, string path)
        => new(1, "title", app, 1, 1, false, false, false, false) { ExecutablePath = path };
}

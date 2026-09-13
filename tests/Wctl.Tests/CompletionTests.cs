namespace Wctl.Tests;

public class CompletionTests
{
    private static string[] Complete(Fakes fakes, params string[] words)
    {
        var result = TestHost.Run(fakes, ["complete", .. words]);
        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        return TestHost.Normalize(result.RawStdout).Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    private static string[] Complete(params string[] words) => Complete(new Fakes(), words);

    /// <summary>What the PowerShell script sends when a fresh word is being typed.</summary>
    private const string End = Wctl.Cli.Completion.EndMarker;

    [Fact]
    public void FirstWord_Empty_OffersCommandsAndScenesButNotEveryApp()
    {
        var fakes = new Fakes();
        fakes.Scenes.Text = "[work]\nvol 15\n";

        var candidates = Complete(fakes, End);

        Assert.Contains("vol", candidates);
        Assert.Contains("hdr", candidates);
        Assert.Contains("scene", candidates);
        Assert.Contains("work", candidates);
        Assert.DoesNotContain("Spotify", candidates);
    }

    [Fact]
    public void FirstWord_WithNoWordsAtAll_BehavesLikeAnEmptyWord()
    {
        Assert.Contains("vol", Complete());
    }

    [Fact]
    public void AnEmptyLastWord_MeansTheSameAsTheEndMarker()
    {
        // Shells that can pass an empty argument do not need the marker.
        Assert.Equal(Complete("hdr", End), Complete("hdr", string.Empty));
    }

    [Fact]
    public void FirstWord_Typed_OffersAppsToo()
    {
        var candidates = Complete("sp");

        Assert.Equal(["Spotify"], candidates);
    }

    [Fact]
    public void FirstWord_FiltersByPrefixAndIncludesAliases()
    {
        var candidates = Complete("m");

        Assert.Contains("move", candidates);
        Assert.Contains("m", candidates);
        Assert.Contains("mute", candidates);
        Assert.Contains("mic", candidates);
        Assert.Contains("monitors", candidates);
        Assert.DoesNotContain("vol", candidates);
    }

    [Fact]
    public void FirstWord_IsCaseInsensitive()
    {
        Assert.Contains("Spotify", Complete("SPOT"));
    }

    [Theory]
    [InlineData("close")]
    [InlineData("x")]
    [InlineData("focus")]
    [InlineData("kill")]
    [InlineData("center")]
    [InlineData("move")]
    [InlineData("max")]
    [InlineData("top")]
    public void WindowCommands_OfferOpenWindowsAndNumbers(string command)
    {
        var candidates = Complete(command, End);

        Assert.Contains("firefox", candidates);
        Assert.Contains("Discord", candidates);
        Assert.Contains("WindowsTerminal", candidates);
        Assert.Contains("1", candidates);
        Assert.Contains("5", candidates);
        Assert.DoesNotContain("6", candidates);
    }

    [Fact]
    public void WindowCommands_ListEachAppOnce()
    {
        var candidates = Complete("close", "fire");

        Assert.Equal(["firefox"], candidates);
    }

    [Fact]
    public void Move_OffersSidesAndMonitors()
    {
        var candidates = Complete("move", "firefox", End);

        Assert.Equal(["left", "right", "monitor"], candidates);
    }

    [Theory]
    [InlineData("monitor")]
    [InlineData("mon")]
    public void Move_AfterMonitor_OffersMonitorNumbers(string word)
    {
        var candidates = Complete("move", "firefox", word, End);

        Assert.Equal(["main", "1", "2"], candidates);
    }

    [Fact]
    public void MaxAndMin_OfferOffAfterTheWindow()
    {
        Assert.Equal(["off"], Complete("max", "firefox", End));
        Assert.Equal(["off"], Complete("min", "firefox", End));
    }

    [Fact]
    public void Top_OffersOnAndOffAfterTheWindow()
    {
        Assert.Equal(["on", "off"], Complete("top", "firefox", End));
    }

    [Fact]
    public void Audio_OffersDeviceNames()
    {
        var candidates = Complete("audio", End);

        Assert.Equal(["Speakers (FiiO K11)", "Speakers (Focusrite USB Audio)", "Headphones (BTD 600)"], candidates);
    }

    [Fact]
    public void Hdr_OffersStatesThenMonitors()
    {
        Assert.Equal(["on", "off", "status", "monitor"], Complete("hdr", End));
        Assert.Equal(["main", "1", "2"], Complete("hdr", "on", "monitor", End));
    }

    [Fact]
    public void Brightness_OffersTheMonitorOption()
    {
        Assert.Equal(["status", "monitor"], Complete("brightness", End));
        Assert.Equal(["main", "1", "2"], Complete("bright", "+5", "monitor", End));
    }

    [Theory]
    [InlineData("vol", new[] { "mute", "unmute", "status" })]
    [InlineData("mute", new[] { "on", "off", "status" })]
    [InlineData("mic", new[] { "mute", "unmute", "status" })]
    [InlineData("apps", new[] { "--refresh" })]
    [InlineData("completion", new[] { "powershell" })]
    public void SimpleCommands_OfferTheirWords(string command, string[] expected)
    {
        Assert.Equal(expected, Complete(command, End));
    }

    [Fact]
    public void Open_OffersAppNames()
    {
        var candidates = Complete("open", "vis");

        Assert.Equal(["Visual Studio 2022", "Visual Studio Code"], candidates);
    }

    [Fact]
    public void Scene_OffersSubcommandsAndSceneNames()
    {
        var fakes = new Fakes();
        fakes.Scenes.Text = "[work]\nvol 15\n\n[gaming]\nhdr on\n";

        Assert.Equal(["list", "show", "new", "edit", "delete", "file", "work", "gaming"], Complete(fakes, "scene", End));
        Assert.Equal(["work", "gaming"], Complete(fakes, "scene", "show", End));
        Assert.Equal(["empty"], Complete(fakes, "scene", "new", "thing", End));
    }

    [Fact]
    public void Scene_WithABrokenFile_StillCompletesSubcommands()
    {
        var fakes = new Fakes();
        fakes.Scenes.Text = "vol 15\n";

        Assert.Equal(["list", "show", "new", "edit", "delete", "file"], Complete(fakes, "scene", End));
    }

    [Fact]
    public void Help_OffersCommandNames()
    {
        var candidates = Complete("help", "sc");

        Assert.Equal(["scene"], candidates);
    }

    [Fact]
    public void UnknownFirstWord_OffersNothing()
    {
        Assert.Empty(Complete("spotify", End));
    }

    [Fact]
    public void CommandsWithoutCompletions_OfferNothing()
    {
        Assert.Empty(Complete("ls", End));
        Assert.Empty(Complete("lock", End));
        Assert.Empty(Complete("awake", End));
    }

    [Fact]
    public void GlobalFlagsInTheLine_DoNotSwallowTheCompletion()
    {
        // "w hdr --json <tab>" must still complete, not print a JSON document.
        var candidates = Complete("hdr", "--json", End);

        Assert.Equal(["on", "off", "status", "monitor"], candidates);
    }

    [Fact]
    public void CompletionScript_RegistersForW()
    {
        var result = TestHost.Run("completion", "powershell");

        Assert.Equal(ExitCodes.Ok, result.ExitCode);
        Assert.Contains("Register-ArgumentCompleter -Native -CommandName w", result.RawStdout);
        Assert.Contains("w complete @words", result.RawStdout);
    }

    [Theory]
    [InlineData("completion")]
    [InlineData("completion", "bash")]
    public void Completion_NeedsASupportedShell(params string[] argv)
    {
        var result = TestHost.Run(argv);

        Assert.Equal(ExitCodes.Failure, result.ExitCode);
        Assert.Equal(string.Empty, result.RawStdout);
    }
}

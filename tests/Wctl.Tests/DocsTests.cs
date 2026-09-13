using Wctl.Cli;
using Wctl.Commands;

namespace Wctl.Tests;

public class DocsTests
{
    [Fact]
    public void CommandsMd_MatchesTheCommandTable()
    {
        var file = Path.Combine(RepoRoot(), "COMMANDS.md");
        Assert.True(File.Exists(file), "COMMANDS.md is missing. Run scripts/update-commands.ps1 and commit the result.");

        var actual = TestHost.Normalize(File.ReadAllText(file));
        var expected = Help.Markdown(Registry.Build());

        if (expected != actual)
        {
            Assert.Fail("COMMANDS.md is out of date. Run scripts/update-commands.ps1 and commit the result.");
        }
    }

    [Fact]
    public void Markdown_EscapesPipesInUsage()
    {
        var table = new CommandTable(
        [
            new CommandSpec
            {
                Name = "volume",
                Aliases = ["vol", "v"],
                Group = Groups.Audio,
                Summary = "Show or set the volume",
                Usage = "vol [n | mute]",
                Run = _ => ExitCodes.Ok,
            },
        ]);

        var markdown = Help.Markdown(table);

        Assert.Contains("## Audio", markdown);
        Assert.Contains("| `w vol [n \\| mute]` | `vol`, `v` | Show or set the volume |", markdown);
    }

    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "wctl.sln")))
        {
            dir = dir.Parent;
        }

        return dir?.FullName ?? throw new InvalidOperationException("wctl.sln was not found above the test directory.");
    }
}

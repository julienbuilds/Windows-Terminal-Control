using Wctl.Cli;
using Wctl.Commands;

namespace Wctl.Tests;

public class CommandTableTests
{
    [Fact]
    public void Find_IsCaseInsensitiveAndKnowsAliases()
    {
        var table = new CommandTable([Spec("close", "x")]);

        Assert.Equal("close", table.Find("CLOSE")?.Name);
        Assert.Equal("close", table.Find("x")?.Name);
        Assert.Null(table.Find("open"));
    }

    [Fact]
    public void DuplicateWords_AreRejected()
    {
        var e = Assert.Throws<InvalidOperationException>(() => new CommandTable([Spec("close", "x"), Spec("exit", "x")]));

        Assert.Contains("'x'", e.Message);
    }

    [Fact]
    public void Registry_BuildsWithoutDuplicates()
    {
        var table = Registry.Build();

        Assert.NotEmpty(table.All);
        Assert.NotNull(table.Find("help"));
    }

    private static CommandSpec Spec(string name, params string[] aliases) => new()
    {
        Name = name,
        Aliases = aliases,
        Group = Groups.Apps,
        Summary = name,
        Usage = name,
        Run = _ => ExitCodes.Ok,
    };
}

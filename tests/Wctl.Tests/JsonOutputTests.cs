using Wctl.Cli;

namespace Wctl.Tests;

public class JsonOutputTests
{
    [Fact]
    public void Facts_AreTypedAndKeysAreStable()
    {
        using var writer = new StringWriter();
        var output = new JsonOutput(writer);

        output.State("Volume", 47, "47%");
        output.State("Muted", false);
        output.State("Output device", "Speakers");
        output.Message("text for people is left out");
        output.Flush();

        Assert.Equal("{\"volume\":47,\"muted\":false,\"output_device\":\"Speakers\"}", writer.ToString().Trim());
    }

    [Fact]
    public void Tables_BecomeArraysWithACurrentFlag()
    {
        using var writer = new StringWriter();
        var output = new JsonOutput(writer);

        output.Table("devices", ["Name"], [["A"], ["B"]], new TableOptions(CurrentRow: 1));
        output.Flush();

        Assert.Equal("{\"devices\":[{\"name\":\"A\",\"current\":false},{\"name\":\"B\",\"current\":true}]}", writer.ToString().Trim());
    }

    [Fact]
    public void Tables_WithoutCurrentRow_HaveNoCurrentFlag()
    {
        using var writer = new StringWriter();
        var output = new JsonOutput(writer);

        output.Table("windows", ["Index", "Title"], [["1", "Firefox"]]);
        output.Flush();

        Assert.Equal("{\"windows\":[{\"index\":\"1\",\"title\":\"Firefox\"}]}", writer.ToString().Trim());
    }

    [Fact]
    public void Error_IsAProperty()
    {
        using var writer = new StringWriter();
        var output = new JsonOutput(writer);

        output.Fail("boom");
        output.Flush();

        Assert.Equal("{\"error\":\"boom\"}", writer.ToString().Trim());
    }
}

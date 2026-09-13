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
    public void Details_AreIncluded()
    {
        using var writer = new StringWriter();
        var output = new JsonOutput(writer);

        output.State("Volume", 47, "47% (muted)");
        output.Detail("Muted", true);
        output.Flush();

        Assert.Equal("{\"volume\":47,\"muted\":true}", writer.ToString().Trim());
    }

    [Fact]
    public void Steps_BecomeAnArrayWithOkFlags()
    {
        using var writer = new StringWriter();
        var output = new JsonOutput(writer);

        output.State("Scene", "work");
        output.StepResult("vol 15", true, "Volume: 15%");
        output.StepResult("close blender", false, "No window matches 'blender'.");
        output.Flush();

        Assert.Equal(
            "{\"scene\":\"work\",\"steps\":[{\"step\":\"vol 15\",\"ok\":true,\"result\":\"Volume: 15%\"},{\"step\":\"close blender\",\"ok\":false,\"result\":\"No window matches \\u0027blender\\u0027.\"}]}",
            writer.ToString().Trim());
    }

    [Fact]
    public void Actions_HaveTargetAndResult()
    {
        using var writer = new StringWriter();
        var output = new JsonOutput(writer);

        output.Action("firefox", "focused");
        output.Flush();

        Assert.Equal("{\"target\":\"firefox\",\"result\":\"focused\"}", writer.ToString().Trim());
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

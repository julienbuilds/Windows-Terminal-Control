using Wctl.Commands.Windows;
using Wctl.Platform;

namespace Wctl.Tests.Commands;

public class GeometryTests
{
    private static readonly Rect Work = new(0, 0, 3072, 1680);

    [Fact]
    public void Halves_SplitTheWorkArea()
    {
        Assert.Equal(new Rect(0, 0, 1536, 1680), Geometry.LeftHalf(Work));
        Assert.Equal(new Rect(1536, 0, 3072, 1680), Geometry.RightHalf(Work));
    }

    [Fact]
    public void Halves_WorkForMonitorsLeftOfThePrimary()
    {
        var work = new Rect(-2560, 272, 0, 1664);

        Assert.Equal(new Rect(-2560, 272, -1280, 1664), Geometry.LeftHalf(work));
        Assert.Equal(new Rect(-1280, 272, 0, 1664), Geometry.RightHalf(work));
    }

    [Fact]
    public void Center_KeepsSizeAndCenters()
    {
        Assert.Equal(Rect.FromSize(936, 440, 1200, 800), Geometry.Center(Work, 1200, 800));
    }

    [Fact]
    public void Center_ShrinksWhatDoesNotFit()
    {
        Assert.Equal(Work, Geometry.Center(Work, 5000, 5000));
    }

    [Fact]
    public void MoveToWorkArea_KeepsTheOffset()
    {
        var frame = Rect.FromSize(100, 100, 1200, 800);
        var to = new Rect(-2560, 272, 0, 1664);

        Assert.Equal(Rect.FromSize(-2460, 372, 1200, 800), Geometry.MoveToWorkArea(frame, Work, to));
    }

    [Fact]
    public void MoveToWorkArea_PullsTheWindowInsideTheNewMonitor()
    {
        var frame = Rect.FromSize(2500, 1200, 1200, 800);
        var to = new Rect(-2560, 272, 0, 1664);

        var moved = Geometry.MoveToWorkArea(frame, Work, to);

        Assert.Equal(Rect.FromSize(-1200, 864, 1200, 800), moved);
        Assert.True(to.Contains(moved));
    }

    [Fact]
    public void FitInto_ShrinksAndShifts()
    {
        var bounds = new Rect(0, 0, 1000, 1000);

        Assert.Equal(Rect.FromSize(0, 0, 1000, 1000), Geometry.FitInto(Rect.FromSize(-50, -50, 2000, 2000), bounds));
        Assert.Equal(Rect.FromSize(0, 0, 300, 300), Geometry.FitInto(Rect.FromSize(-50, -50, 300, 300), bounds));
        Assert.Equal(Rect.FromSize(700, 700, 300, 300), Geometry.FitInto(Rect.FromSize(900, 900, 300, 300), bounds));
    }

    [Fact]
    public void Rect_PrintsPositionAndSize()
    {
        Assert.Equal("100,200 1200x800", Rect.FromSize(100, 200, 1200, 800).ToString());
    }
}

using Wctl.Commands.Apps;
using Wctl.Platform;

namespace Wctl.Tests.Commands;

public class AppCatalogTests
{
    private readonly Fakes fakes = new();

    private AppCatalog Catalog() => new(fakes.Shell, fakes.AppCache, fakes.Clock);

    [Fact]
    public void FirstUse_AsksWindowsAndWritesTheCache()
    {
        var apps = Catalog().Apps;

        Assert.Equal(fakes.Shell.Apps, apps);
        Assert.Equal(1, fakes.Shell.InstalledAppsCalls);
        Assert.Contains("Visual Studio Code\t", Assert.Single(fakes.AppCache.Written));
    }

    [Fact]
    public void SecondRun_ReadsTheCacheAndDoesNotAskWindows()
    {
        Catalog().Refresh();
        fakes.Shell.Apps.Clear();

        var apps = Catalog().Apps;

        Assert.Equal(1, fakes.Shell.InstalledAppsCalls);
        Assert.Equal(7, apps.Count);
        Assert.Contains(apps, a => a.Name == "Spotify" && a.Id == FakeShell.SpotifyId);
    }

    [Fact]
    public void CacheOlderThanADay_IsRebuilt()
    {
        Catalog().Refresh();
        fakes.Clock.Now = fakes.Clock.Now.AddHours(25);

        _ = Catalog().Apps;

        Assert.Equal(2, fakes.Shell.InstalledAppsCalls);
    }

    [Fact]
    public void CacheFromTheFuture_IsRebuilt()
    {
        Catalog().Refresh();
        fakes.Clock.Now = fakes.Clock.Now.AddHours(-3);

        _ = Catalog().Apps;

        Assert.Equal(2, fakes.Shell.InstalledAppsCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("nonsense")]
    [InlineData("# wctl app cache, written not-a-date\nA\tB\tC\n")]
    [InlineData("# wctl app cache, written 2026-09-13T12:00:00.0000000+02:00\n")]
    public void UnusableCache_IsRebuilt(string? text)
    {
        fakes.AppCache.Text = text;

        var apps = Catalog().Apps;

        Assert.Equal(1, fakes.Shell.InstalledAppsCalls);
        Assert.Equal(fakes.Shell.Apps, apps);
    }

    [Fact]
    public void FindMiss_RefreshesOnceThenGivesUp()
    {
        Catalog().Refresh();
        var catalog = Catalog();

        Assert.Null(catalog.Find("blender"));
        Assert.Null(catalog.Find("blender"));
        Assert.Equal(2, fakes.Shell.InstalledAppsCalls);
    }

    [Fact]
    public void FindMiss_SeesAnAppInstalledSinceTheCacheWasWritten()
    {
        Catalog().Refresh();
        fakes.Shell.Apps.Add(new AppEntry("Blender", "blender-id", "blender"));

        Assert.Equal("blender-id", Catalog().Find("blender")?.Id);
    }

    [Fact]
    public void FindHit_DoesNotAskWindowsAgain()
    {
        Catalog().Refresh();

        Assert.Equal(FakeShell.SpotifyId, Catalog().Find("spotify")?.Id);
        Assert.Equal(1, fakes.Shell.InstalledAppsCalls);
    }

    [Fact]
    public void FormatAndParse_RoundTrip()
    {
        var now = fakes.Clock.Now;
        var apps = new List<AppEntry> { new("A b", "id one", "exe"), new("C", "id two", string.Empty) };

        var parsed = AppCatalog.TryParse(AppCatalog.Format(apps, now), now, TimeSpan.FromHours(24));

        Assert.Equal(apps, parsed);
    }

    [Fact]
    public void Format_SkipsEntriesWithATab()
    {
        var text = AppCatalog.Format([new("Good", "id", "exe"), new("Ba\td", "id", "exe")], fakes.Clock.Now);

        Assert.Single(AppCatalog.TryParse(text, fakes.Clock.Now, TimeSpan.FromHours(24))!);
    }
}

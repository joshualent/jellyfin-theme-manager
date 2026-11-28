using ThemeManager.Core;
using Xunit;

namespace ThemeManager.Tests;

public sealed class ServerImagesTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "theme-manager-tests", Guid.NewGuid().ToString("N"));
    public void Dispose() { if (Directory.Exists(_root)) Directory.Delete(_root, true); }

    [Fact]
    public void BannerGoesToTheLibraryNamedForItsType()
    {
        var assigned = LibraryBanners.Assign([
            new("a", "movies", "Børnefilm"), new("b", "movies", "Film"), new("c", "tvshows", "Anime"),
            new("d", "tvshows", "TV-serier"), new("e", "boxsets", "Mine samlinger"), new("f", "music", "Musik"), new("g", null, "Blandet"),
        ]);
        Assert.Equal(new Dictionary<string, string> { ["b"] = "movies", ["d"] = "tvShows", ["e"] = "collections" }, assigned);
    }

    [Fact]
    public void AdministratorChoiceWinsAndLeavesTheAutomaticRule()
    {
        var assigned = LibraryBanners.Assign([new("a", "movies", "Film"), new("b", "movies", "Classics"), new("c", "music", "Musik"), new("d", "tvshows", "TV")],
            new Dictionary<string, string> { ["a"] = LibraryBanners.Generic, ["c"] = "movies", ["d"] = LibraryBanners.None, ["x"] = "bogus" });
        Assert.Equal(new Dictionary<string, string> { ["b"] = "movies", ["c"] = "movies", ["d"] = LibraryBanners.None }, assigned);
    }

    [Fact]
    public void LiveTvGetsItsOwnBanner()
        => Assert.Equal("liveTv", LibraryBanners.Assign([new("a", "livetv", "Live TV"), new("b", "movies", "Film")])["a"]);

    [Fact]
    public void FirstLibraryOfATypeWinsWithoutANamedOne()
        => Assert.Equal("movies", LibraryBanners.Assign([new("a", "movies", "Kids"), new("b", "movies", "Classics")])["a"]);

    [Fact]
    public void StateRoundTripsAndBackupCopiesTheImage()
    {
        var state = new ServerImageState(_root);
        Assert.Empty(state.Load());
        var source = Path.Combine(_root, "current.JPG"); Directory.CreateDirectory(_root); File.WriteAllText(source, "original");
        var id = Guid.NewGuid().ToString();
        var backup = state.Backup(id, source);
        Assert.Equal("original", File.ReadAllText(backup));
        Assert.EndsWith(".jpg", backup);
        state.Save(new() { [id] = new() { Backup = backup, Applied = "rev1" }, ["other"] = new() { Applied = "rev2" } });
        var loaded = state.Load();
        Assert.Equal(backup, loaded[id].Backup); Assert.Equal("rev1", loaded[id].Applied); Assert.Null(loaded["other"].Backup);
    }

    [Fact]
    public void BackupRejectsNonGuidIds() => Assert.Throws<ArgumentException>(() => new ServerImageState(_root).Backup("../x", "y.png"));

    [Fact]
    public void PrimaryKeepsTheBareLibraryIdOtherTypesGetASuffix()
    {
        var id = Guid.NewGuid().ToString();
        Assert.Equal(id, AppImageKeys.For(id, AppImageKeys.Primary));
        Assert.Equal(id + ":Backdrop", AppImageKeys.For(id, "Backdrop"));
        Assert.Equal((id, "Primary"), AppImageKeys.Parse(id));
        Assert.Equal((id, "Thumb"), AppImageKeys.Parse(AppImageKeys.For(id, "Thumb")));
    }

    [Fact]
    public void BackupsOfDifferentImageTypesDoNotOverwriteEachOther()
    {
        var state = new ServerImageState(_root);
        Directory.CreateDirectory(_root);
        var primary = Path.Combine(_root, "p.png"); File.WriteAllText(primary, "primary");
        var backdrop = Path.Combine(_root, "b.png"); File.WriteAllText(backdrop, "backdrop");
        var id = Guid.NewGuid().ToString();
        var first = state.Backup(id, primary);
        var second = state.Backup(id, backdrop, "Backdrop");
        Assert.NotEqual(first, second);
        Assert.Equal("primary", File.ReadAllText(first));
        Assert.Equal("backdrop", File.ReadAllText(second));
        Assert.Throws<ArgumentException>(() => state.Backup(id, backdrop, "../Backdrop"));
    }

    [Fact]
    public void SplashRecordRoundTripsAndNullRemovesIt()
    {
        var state = new ServerImageState(_root);
        Assert.Null(state.LoadSplash());
        state.SaveSplash(new() { OriginalLocation = "/data/splashscreen-upload.png", OriginalEnabled = false, Path = "/x/splash.webp", Applied = "rev" });
        var loaded = state.LoadSplash();
        Assert.Equal("/data/splashscreen-upload.png", loaded!.OriginalLocation);
        Assert.False(loaded.OriginalEnabled);
        Assert.Equal("rev", loaded.Applied);
        state.SaveSplash(null);
        Assert.Null(state.LoadSplash());
    }

    [Fact]
    public void UnreadableStateIsEmptyNotAnError()
    {
        Directory.CreateDirectory(_root); File.WriteAllText(Path.Combine(_root, "state.json"), "{ broken");
        Assert.Empty(new ServerImageState(_root).Load());
    }
}

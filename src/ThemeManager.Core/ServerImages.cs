using System.Text.Json;

namespace ThemeManager.Core;

/// <summary>A Jellyfin library as the server sees it.</summary>
public sealed record LibraryInfo(string Id, string? CollectionType, string Name);

/// <summary>
/// Which library gets which theme banner. The same rule as the web client (client-core.mjs, librarySlots): one library
/// per banner type uses the theme banner, the one whose name matches the type (e.g. "Film" rather than "Børnefilm"),
/// otherwise the first. Libraries without a matching type get no banner.
/// </summary>
public static class LibraryBanners
{
    private static readonly Dictionary<string, string> BannerTypes = new(StringComparer.Ordinal)
    {
        ["movies"] = "movies", ["tvshows"] = "tvShows", ["boxsets"] = "collections", ["livetv"] = "liveTv",
    };
    private static readonly Dictionary<string, string[]> TypeNames = new(StringComparer.Ordinal)
    {
        ["movies"] = ["movies", "movie", "film", "films", "filmer", "spillefilm"],
        ["tvShows"] = ["tvshows", "tv", "series", "serier", "shows", "tvserier"],
        ["collections"] = ["collections", "samlinger", "boxsets"],
        ["liveTv"] = ["livetv", "live", "tvguide"],
    };

    private static string Plain(string? name) => new((name ?? "").ToLowerInvariant().Where(char.IsLetterOrDigit).ToArray());

    /// <summary>An administrator's choice: the theme's background with the library name.</summary>
    public const string Generic = "generic";
    /// <summary>An administrator's choice: leave the library's own image.</summary>
    public const string None = "none";
    /// <summary>What an administrator may choose for one library; no choice means automatic.</summary>
    public static readonly IReadOnlyList<string> Choices = ["movies", "tvShows", "collections", "liveTv", Generic, None];

    /// <summary>
    /// Library id to banner slot, for the libraries that get one. A library the administrator chose for gets that choice
    /// (<see cref="None"/> included; <see cref="Generic"/> means no entry) and takes no part in the automatic rule.
    /// </summary>
    public static IReadOnlyDictionary<string, string> Assign(IEnumerable<LibraryInfo> libraries, IReadOnlyDictionary<string, string>? chosen = null)
    {
        var owner = new Dictionary<string, (string Id, bool Named)>(StringComparer.Ordinal);
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var library in libraries)
        {
            if (chosen?.GetValueOrDefault(library.Id) is { } choice && Choices.Contains(choice))
            {
                if (choice != Generic) result[library.Id] = choice;
                continue;
            }
            if (library.CollectionType is null || !BannerTypes.TryGetValue(library.CollectionType, out var slot)) continue;
            var named = TypeNames[slot].Contains(Plain(library.Name));
            if (!owner.TryGetValue(slot, out var current) || (named && !current.Named)) owner[slot] = (library.Id, named);
        }
        foreach (var (slot, library) in owner) result[library.Id] = slot;
        return result;
    }
}

/// <summary>
/// Key of one library image in <see cref="ServerImageState"/>. The Primary image keeps the bare library id (the key
/// older versions wrote); other image types are "libraryId:Type", e.g. "...:Backdrop".
/// </summary>
public static class AppImageKeys
{
    public const string Primary = "Primary";
    public static string For(string libraryId, string imageType) =>
        imageType == Primary ? libraryId : libraryId + ":" + imageType;
    public static (string LibraryId, string ImageType) Parse(string key)
    {
        var colon = key.IndexOf(':', StringComparison.Ordinal);
        return colon < 0 ? (key, Primary) : (key[..colon], key[(colon + 1)..]);
    }
}

/// <summary>What the plugin did to the server's splash screen, so it can be undone.</summary>
public sealed record SplashRecord
{
    /// <summary>Branding's custom splash screen path before the plugin set its own; null for Jellyfin's generated one.</summary>
    public string? OriginalLocation { get; init; }
    /// <summary>Whether the splash screen was switched on before the plugin switched it on.</summary>
    public bool OriginalEnabled { get; init; }
    /// <summary>The splash file the plugin set, and the theme asset revision it came from.</summary>
    public string? Path { get; init; }
    public string? Applied { get; init; }
}

/// <summary>What the plugin did to one library's image, so it can be undone.</summary>
public sealed record LibraryImageRecord
{
    /// <summary>Backup of the image the library had before the plugin first changed it; null if it had none.</summary>
    public string? Backup { get; init; }
    /// <summary>Revision of the theme banner now set, so an unchanged banner is not written again.</summary>
    public string? Applied { get; init; }
}

/// <summary>Per-library records in one JSON file, written atomically.</summary>
public sealed class ServerImageState(string root)
{
    public string Root { get; } = Path.GetFullPath(root);
    private string FilePath => Path.Combine(Root, "state.json");

    public Dictionary<string, LibraryImageRecord> Load()
    {
        try { return File.Exists(FilePath) ? JsonSerializer.Deserialize<Dictionary<string, LibraryImageRecord>>(File.ReadAllText(FilePath), ThemeJson.Options) ?? [] : []; }
        // An unreadable state must not lose the backups: the backup files stay in Root and are listed in the log.
        catch (JsonException) { return []; }
    }

    public void Save(Dictionary<string, LibraryImageRecord> state)
    {
        Directory.CreateDirectory(Root);
        var temporary = FilePath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(state, ThemeJson.Options));
        File.Move(temporary, FilePath, overwrite: true);
    }

    private string SplashPath => Path.Combine(Root, "splash.json");

    public SplashRecord? LoadSplash()
    {
        try { return File.Exists(SplashPath) ? JsonSerializer.Deserialize<SplashRecord>(File.ReadAllText(SplashPath), ThemeJson.Options) : null; }
        catch (JsonException) { return null; }
    }

    /// <summary>Saves the splash record; null removes it.</summary>
    public void SaveSplash(SplashRecord? record)
    {
        if (record is null) { if (File.Exists(SplashPath)) File.Delete(SplashPath); return; }
        Directory.CreateDirectory(Root);
        var temporary = SplashPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(record, ThemeJson.Options));
        File.Move(temporary, SplashPath, overwrite: true);
    }

    /// <summary>Copies the library's current image into the backup folder and returns the copy's path.</summary>
    public string Backup(string libraryId, string imagePath, string imageType = AppImageKeys.Primary)
    {
        Directory.CreateDirectory(Root);
        if (!Guid.TryParse(libraryId, out var id)) throw new ArgumentException("A library id is a GUID.", nameof(libraryId));
        if (!imageType.All(char.IsAsciiLetter)) throw new ArgumentException("An image type is letters only.", nameof(imageType));
        var suffix = imageType == AppImageKeys.Primary ? "" : "-" + imageType.ToLowerInvariant();
        var target = Path.Combine(Root, "original-" + id.ToString("N") + suffix + Path.GetExtension(imagePath).ToLowerInvariant());
        File.Copy(imagePath, target, overwrite: true);
        return target;
    }
}

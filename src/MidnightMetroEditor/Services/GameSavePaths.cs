using System.Text.Json;

namespace MidnightMetroEditor.Services;

/// <summary>
/// Mirrors <c>MidnightMetro.Simulation.Saves.SaveService</c> paths on Windows
/// (Unity <c>Application.persistentDataPath</c>).
/// </summary>
public static class GameSavePaths
{
    public const string CompanyName = "Midnight Line Studio";
    public const string ProductName = "Midnight Metro";
    public const string SaveFileName = "midnight_metro_save.json";
    public const string SavesFolderName = "Saves";
    public const string LegacySaveFileName = "midnight_metro_week1.json";

    public static string PersistentDataPath =>
        Path.GetFullPath(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "..",
            "LocalLow",
            CompanyName,
            ProductName));

    public static string SavesRoot => Path.Combine(PersistentDataPath, SavesFolderName);

    public static string PrimarySavePath => Path.Combine(PersistentDataPath, SaveFileName);

    public static string LegacySavePath => Path.Combine(PersistentDataPath, LegacySaveFileName);

    public static string? GetMostRecentSavePath()
    {
        var listings = ListSaveFiles();
        return listings.Count > 0 ? listings[0].Path : null;
    }

    public static IReadOnlyList<GameSaveListing> ListSaveFiles()
    {
        var results = new List<GameSaveListing>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void TryAdd(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !seen.Add(path) || !File.Exists(path))
                return;

            if (TryReadListing(path, out var listing))
                results.Add(listing);
        }

        TryAdd(PrimarySavePath);
        TryAdd(LegacySavePath);

        if (Directory.Exists(SavesRoot))
        {
            foreach (var path in Directory.GetFiles(SavesRoot, "*.json", SearchOption.AllDirectories))
                TryAdd(path);
        }

        if (Directory.Exists(PersistentDataPath))
        {
            foreach (var path in Directory.GetFiles(PersistentDataPath, "midnight_metro*.json"))
                TryAdd(path);
        }

        results.Sort(CompareRecency);
        return results;
    }

    public static bool TryReadListing(string path, out GameSaveListing listing)
    {
        listing = null!;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        try
        {
            var json = SaveCompression.ReadSaveText(path);
            if (!MetroSaveGameFormat.IsGameSave(json))
                return false;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("session", out var session))
                return false;

            var fileName = Path.GetFileName(path);
            var folderName = Path.GetFileName(Path.GetDirectoryName(path));
            var cityName = session.TryGetProperty("cityName", out var cityEl)
                ? cityEl.GetString()
                : null;
            var day = session.TryGetProperty("day", out var dayEl) ? dayEl.GetInt32() : 0;
            var treasury = session.TryGetProperty("treasury", out var treasuryEl) ? treasuryEl.GetInt32() : 0;
            var isAutoSave = session.TryGetProperty("isAutoSave", out var autoEl) && autoEl.GetInt32() != 0;

            var gridWidth = 0;
            var gridHeight = 0;
            if (root.TryGetProperty("grid", out var grid)
                && grid.TryGetProperty("width", out var wEl)
                && grid.TryGetProperty("height", out var hEl))
            {
                gridWidth = wEl.GetInt32();
                gridHeight = hEl.GetInt32();
            }

            var residentCount = 0;
            if (root.TryGetProperty("residents", out var residents)
                && residents.TryGetProperty("rosterId", out var roster)
                && roster.ValueKind == JsonValueKind.Array)
                residentCount = roster.GetArrayLength();

            listing = new GameSaveListing
            {
                Path = path,
                FileName = fileName,
                FolderName = folderName,
                CityName = string.IsNullOrWhiteSpace(cityName) ? null : cityName.Trim(),
                Day = day,
                GridWidth = gridWidth,
                GridHeight = gridHeight,
                Treasury = treasury,
                ResidentCount = residentCount,
                SaveVersion = root.TryGetProperty("version", out var versionEl) ? versionEl.GetInt32() : 0,
                SavedUtc = root.TryGetProperty("savedUtc", out var savedEl) ? savedEl.GetString() : null,
                IsBackup = fileName.Contains(".bak_", StringComparison.OrdinalIgnoreCase),
                IsAutoSave = isAutoSave || fileName.StartsWith("autosave_", StringComparison.OrdinalIgnoreCase)
            };
            return true;
        }
        catch
        {
            return false;
        }
    }

    static int CompareRecency(GameSaveListing a, GameSaveListing b)
    {
        var bySaved = string.Compare(b.SavedUtc, a.SavedUtc, StringComparison.Ordinal);
        if (bySaved != 0)
            return bySaved;

        return SafeWriteTime(b.Path).CompareTo(SafeWriteTime(a.Path));
    }

    static DateTime SafeWriteTime(string path)
    {
        try
        {
            return File.GetLastWriteTimeUtc(path);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}

public sealed class GameSaveListing
{
    public string Path { get; init; } = "";
    public string? FileName { get; init; }
    public string? FolderName { get; init; }
    public string? CityName { get; init; }
    public int Day { get; init; }
    public int GridWidth { get; init; }
    public int GridHeight { get; init; }
    public int ResidentCount { get; init; }
    public int Treasury { get; init; }
    public int SaveVersion { get; init; }
    public string? SavedUtc { get; init; }
    public bool IsBackup { get; init; }
    public bool IsAutoSave { get; init; }

    public string DisplayTitle
    {
        get
        {
            var title = string.IsNullOrWhiteSpace(CityName) ? FolderName ?? FileName ?? "Saved city" : CityName.Trim();
            if (IsAutoSave)
                title += " · auto";
            return IsBackup ? $"{title} (backup)" : title;
        }
    }
}

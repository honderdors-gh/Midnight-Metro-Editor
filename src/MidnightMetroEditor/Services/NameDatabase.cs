using System.Text.Json;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public sealed class NameDatabase
{
    readonly Dictionary<string, EthnicNamePool> _pools = new(StringComparer.OrdinalIgnoreCase);

    public static string DefaultNamesPath =>
        ResolveFirstExisting(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "Midnight Metro", "Midnight Metro", "Assets", "MidnightMetro", "Resources", "MidnightMetro", "Names", "citizen_names.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "playground-1", "Assets", "CitySim", "Resources", "CitySim", "Names", "citizen_names.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "MidnightMetro", "Resources", "MidnightMetro", "Names", "citizen_names.json"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "CitySim", "Resources", "CitySim", "Names", "citizen_names.json"));

    static string ResolveFirstExisting(params string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (File.Exists(full))
                return full;
        }

        return Path.GetFullPath(candidates[0]);
    }

    public static string ResolveGameNamesPath()
    {
        var configured = EditorSettings.Load().NamesJsonPath;
        if (!string.IsNullOrWhiteSpace(configured) && File.Exists(configured))
            return configured;

        var fromRepo = Path.GetFullPath(DefaultNamesPath);
        if (File.Exists(fromRepo))
            return fromRepo;

        return configured ?? fromRepo;
    }

    public void Load(string path)
    {
        _pools.Clear();
        if (!File.Exists(path))
            return;

        using var stream = File.OpenRead(path);
        using var doc = JsonDocument.Parse(stream);
        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            var pool = new EthnicNamePool
            {
                MaleFirst = ReadStringArray(prop.Value, "maleFirstNames"),
                FemaleFirst = ReadStringArray(prop.Value, "femaleFirstNames"),
                Family = ReadStringArray(prop.Value, "familyNames")
            };
            _pools[prop.Name] = pool;
        }
    }

    static string[] ReadStringArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return arr.EnumerateArray()
            .Select(e => e.GetString() ?? "?")
            .ToArray();
    }

    public string ResolveDisplayName(int rosterId, int givenNameId, int familyNameId, int? appearanceSex, int? appearanceEthnicity, string? fallbackDisplayName = null)
    {
        if (!string.IsNullOrWhiteSpace(fallbackDisplayName))
            return fallbackDisplayName.Trim();

        var ethKey = appearanceEthnicity switch
        {
            0 => "caucasian",
            1 => "asian",
            2 => "african",
            _ => "caucasian"
        };

        if (!_pools.TryGetValue(ethKey, out var pool))
            _pools.TryGetValue("caucasian", out pool);

        pool ??= EthnicNamePool.Empty;

        var female = appearanceSex == 1;
        var firstList = female ? pool.FemaleFirst : pool.MaleFirst;
        var first = firstList.Length > 0 ? firstList[Math.Abs(givenNameId) % firstList.Length] : "?";
        var fam = pool.Family.Length > 0 ? pool.Family[Math.Abs(familyNameId) % pool.Family.Length] : "?";
        var sex = female ? "F" : "M";
        return $"{first} {fam} (#{rosterId}, {sex})";
    }

    sealed class EthnicNamePool
    {
        public static EthnicNamePool Empty { get; } = new();
        public string[] MaleFirst { get; init; } = Array.Empty<string>();
        public string[] FemaleFirst { get; init; } = Array.Empty<string>();
        public string[] Family { get; init; } = Array.Empty<string>();
    }
}

public static class EditorSettings
{
    static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "MidnightMetroEditor",
        "settings.json");

    public static EditorSettingsData Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return new EditorSettingsData();

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<EditorSettingsData>(json) ?? new EditorSettingsData();
        }
        catch
        {
            return new EditorSettingsData();
        }
    }

    public static void Save(EditorSettingsData data)
    {
        var dir = Path.GetDirectoryName(SettingsPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed class EditorSettingsData
{
    public string? NamesJsonPath { get; set; }
    public string? LastSaveDirectory { get; set; }
}

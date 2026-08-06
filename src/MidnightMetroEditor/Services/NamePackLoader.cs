using System.Text.Json;

namespace MidnightMetroEditor.Services;

/// <summary>
/// Loads citizen name packs (built-in + workshop) and city name packs for display / geo hints.
/// Mirrors the game's <c>WorkshopNamePackRegistry</c> and <c>CityNamePackRegistry</c> without Unity.
/// </summary>
public sealed class NamePackLoader
{
    readonly Dictionary<string, EthnicNamePool> _pools = new(StringComparer.OrdinalIgnoreCase);
    readonly List<CityNamePackManifest> _cityPacks = new();
    readonly List<string> _warnings = new();
    string _sourceSummary = "not loaded";

    public IReadOnlyList<string> Warnings => _warnings;
    public string SourceSummary => _sourceSummary;

    public void LoadAll(string? legacyCitizenNamesPath = null)
    {
        _pools.Clear();
        _cityPacks.Clear();
        _warnings.Clear();

        var citizenPacksLoaded = LoadCitizenNamePacks();
        LoadCityNamePacks();

        if (!citizenPacksLoaded || _pools.Count == 0)
        {
            var legacy = !string.IsNullOrWhiteSpace(legacyCitizenNamesPath) && File.Exists(legacyCitizenNamesPath)
                ? legacyCitizenNamesPath
                : NameDatabase.DefaultNamesPath;
            LoadLegacyCitizenNamesFile(legacy);
        }

        _sourceSummary = _pools.Count > 0
            ? $"name packs ({_pools.Values.Sum(p => p.MaleFirst.Length + p.FemaleFirst.Length + p.Family.Length)} names)"
            : "no names loaded";
    }

    bool LoadCitizenNamePacks()
    {
        var gameRoot = GameResourcePaths.FindGameResourcesRoot();
        if (gameRoot == null)
            return false;

        var indexPath = Path.Combine(gameRoot, "Names", "name_pack_index.json");
        if (!File.Exists(indexPath))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
            if (!doc.RootElement.TryGetProperty("packPaths", out var paths) || paths.ValueKind != JsonValueKind.Array)
                return false;

            var loaded = 0;
            foreach (var pathEl in paths.EnumerateArray())
            {
                var resourcePath = pathEl.GetString();
                if (string.IsNullOrWhiteSpace(resourcePath))
                    continue;

                var rel = resourcePath.Replace("MidnightMetro/", "").Replace('/', Path.DirectorySeparatorChar);
                var packPath = Path.Combine(gameRoot, rel + ".json");
                if (!File.Exists(packPath))
                {
                    _warnings.Add($"Missing pack: {packPath}");
                    continue;
                }

                if (TryMergeCitizenPack(packPath))
                    loaded++;
            }

            LoadWorkshopCitizenPacks();
            return loaded > 0 || _pools.Count > 0;
        }
        catch (Exception ex)
        {
            _warnings.Add($"name_pack_index: {ex.Message}");
            return false;
        }
    }

    void LoadWorkshopCitizenPacks()
    {
        var root = Path.Combine(GameSavePaths.PersistentDataPath, "Workshop", "Names");
        if (!Directory.Exists(root))
            return;

        foreach (var dir in Directory.GetDirectories(root))
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (!File.Exists(manifestPath))
                continue;

            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
                var category = doc.RootElement.TryGetProperty("category", out var catEl)
                    ? catEl.GetString()
                    : null;
                if (!string.Equals(category, "citizen_names", StringComparison.OrdinalIgnoreCase))
                    continue;

                TryMergeCitizenPack(manifestPath);
            }
            catch (Exception ex)
            {
                _warnings.Add($"Workshop pack {dir}: {ex.Message}");
            }
        }
    }

    bool TryMergeCitizenPack(string path)
    {
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            var root = doc.RootElement;
            MergeEthnicPool(root, "caucasian");
            MergeEthnicPool(root, "asian");
            MergeEthnicPool(root, "african");
            return true;
        }
        catch (Exception ex)
        {
            _warnings.Add($"{Path.GetFileName(path)}: {ex.Message}");
            return false;
        }
    }

    void MergeEthnicPool(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var poolEl) || poolEl.ValueKind != JsonValueKind.Object)
            return;

        if (!_pools.TryGetValue(key, out var pool))
        {
            pool = new EthnicNamePool();
            _pools[key] = pool;
        }

        pool.MaleFirst = MergeArrays(pool.MaleFirst, ReadStringArray(poolEl, "maleFirstNames"));
        pool.FemaleFirst = MergeArrays(pool.FemaleFirst, ReadStringArray(poolEl, "femaleFirstNames"));
        pool.Family = MergeArrays(pool.Family, ReadStringArray(poolEl, "familyNames"));
    }

    void LoadLegacyCitizenNamesFile(string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            using var doc = JsonDocument.Parse(File.OpenRead(path));
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (!_pools.TryGetValue(prop.Name, out var pool))
                {
                    pool = new EthnicNamePool();
                    _pools[prop.Name] = pool;
                }

                pool.MaleFirst = ReadStringArray(prop.Value, "maleFirstNames");
                pool.FemaleFirst = ReadStringArray(prop.Value, "femaleFirstNames");
                pool.Family = ReadStringArray(prop.Value, "familyNames");
            }
        }
        catch (Exception ex)
        {
            _warnings.Add($"citizen_names.json: {ex.Message}");
        }
    }

    void LoadCityNamePacks()
    {
        var gameRoot = GameResourcePaths.FindGameResourcesRoot();
        if (gameRoot == null)
            return;

        var indexPath = Path.Combine(gameRoot, "CityNames", "city_name_index.json");
        if (!File.Exists(indexPath))
            return;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(indexPath));
            if (!doc.RootElement.TryGetProperty("packPaths", out var paths))
                return;

            foreach (var pathEl in paths.EnumerateArray())
            {
                var resourcePath = pathEl.GetString();
                if (string.IsNullOrWhiteSpace(resourcePath))
                    continue;

                var rel = resourcePath.Replace("MidnightMetro/", "").Replace('/', Path.DirectorySeparatorChar);
                var packPath = Path.Combine(gameRoot, rel + ".json");
                if (File.Exists(packPath))
                    TryLoadCityPack(packPath);
            }

            var workshopRoot = Path.Combine(GameSavePaths.PersistentDataPath, "Workshop", "CityNames");
            if (Directory.Exists(workshopRoot))
            {
                foreach (var dir in Directory.GetDirectories(workshopRoot))
                {
                    var manifest = Path.Combine(dir, "manifest.json");
                    if (File.Exists(manifest))
                        TryLoadCityPack(manifest);
                }
            }
        }
        catch (Exception ex)
        {
            _warnings.Add($"city_name_index: {ex.Message}");
        }
    }

    void TryLoadCityPack(string path)
    {
        try
        {
            var json = File.ReadAllText(path);
            var pack = JsonSerializer.Deserialize<CityNamePackManifest>(json, JsonOptions);
            if (pack?.names != null && pack.names.Length > 0)
                _cityPacks.Add(pack);
        }
        catch (Exception ex)
        {
            _warnings.Add($"City pack {Path.GetFileName(path)}: {ex.Message}");
        }
    }

    public EthnicNamePool GetPool(int appearanceEthnicity) =>
        appearanceEthnicity switch
        {
            1 => GetPoolByKey("asian"),
            2 => GetPoolByKey("african"),
            _ => GetPoolByKey("caucasian")
        };

    EthnicNamePool GetPoolByKey(string key)
    {
        if (_pools.TryGetValue(key, out var pool))
            return pool;
        return _pools.Values.FirstOrDefault() ?? EthnicNamePool.Empty;
    }

    public string ResolveFirstName(int givenNameId, int appearanceSex, int appearanceEthnicity)
    {
        var pool = GetPool(appearanceEthnicity);
        var list = appearanceSex == 1 ? pool.FemaleFirst : pool.MaleFirst;
        return list.Length > 0 ? list[Math.Abs(givenNameId) % list.Length] : "?";
    }

    public string ResolveFamilyName(int familyNameId, int appearanceEthnicity)
    {
        var pool = GetPool(appearanceEthnicity);
        return pool.Family.Length > 0 ? pool.Family[Math.Abs(familyNameId) % pool.Family.Length] : "?";
    }

    public bool TryResolveCityPack(string cityName, out float latitude, out float longitude, out string packLabel)
    {
        latitude = 0f;
        longitude = 0f;
        packLabel = "";
        if (string.IsNullOrWhiteSpace(cityName))
            return false;

        var trimmed = cityName.Trim();
        foreach (var pack in _cityPacks)
        {
            if (pack.names == null)
                continue;

            foreach (var entry in pack.names)
            {
                if (entry?.name == null)
                    continue;
                if (!string.Equals(entry.name.Trim(), trimmed, StringComparison.OrdinalIgnoreCase))
                    continue;

                latitude = pack.centerLatitude;
                longitude = pack.centerLongitude;
                packLabel = string.IsNullOrWhiteSpace(pack.displayName) ? pack.packId ?? "pack" : pack.displayName;
                return true;
            }
        }

        return false;
    }

    static string[] ReadStringArray(JsonElement parent, string name)
    {
        if (!parent.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
            return Array.Empty<string>();

        return arr.EnumerateArray()
            .Select(e => e.GetString() ?? "?")
            .ToArray();
    }

    static string[] MergeArrays(string[] existing, string[] incoming)
    {
        if (incoming.Length == 0)
            return existing;
        if (existing.Length == 0)
            return incoming;

        var set = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        var merged = new List<string>(existing);
        foreach (var name in incoming)
        {
            if (set.Add(name))
                merged.Add(name);
        }

        return merged.ToArray();
    }

    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
}

public sealed class EthnicNamePool
{
    public static EthnicNamePool Empty { get; } = new();
    public string[] MaleFirst { get; set; } = Array.Empty<string>();
    public string[] FemaleFirst { get; set; } = Array.Empty<string>();
    public string[] Family { get; set; } = Array.Empty<string>();
}

public sealed class CityNamePackManifest
{
    public string? packId { get; set; }
    public string? displayName { get; set; }
    public float centerLatitude { get; set; }
    public float centerLongitude { get; set; }
    public float radiusKm { get; set; } = 600f;
    public CityNamePackEntry[]? names { get; set; }
}

public sealed class CityNamePackEntry
{
    public string? name { get; set; }
}

public static class GameResourcePaths
{
    public static string? FindGameResourcesRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "Midnight Metro", "Midnight Metro", "Assets", "MidnightMetro", "Resources", "MidnightMetro"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Assets", "MidnightMetro", "Resources", "MidnightMetro"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", "playground-1", "Assets", "CitySim", "Resources", "CitySim")
        };

        foreach (var candidate in candidates)
        {
            var full = Path.GetFullPath(candidate);
            if (Directory.Exists(full))
                return full;
        }

        return null;
    }
}

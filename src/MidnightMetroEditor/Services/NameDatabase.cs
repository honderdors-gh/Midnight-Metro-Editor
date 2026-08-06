using System.Text.Json;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public sealed class NameDatabase
{
    readonly NamePackLoader _packs = new();

    public NamePackLoader Packs => _packs;

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

    public void Load(string? legacyPath = null)
    {
        var path = !string.IsNullOrWhiteSpace(legacyPath) ? legacyPath : ResolveGameNamesPath();
        _packs.LoadAll(path);
    }

    public string ResolveDisplayName(int rosterId, int givenNameId, int familyNameId, int? appearanceSex, int? appearanceEthnicity, string? fallbackDisplayName = null)
    {
        if (!string.IsNullOrWhiteSpace(fallbackDisplayName))
            return fallbackDisplayName.Trim();

        var sex = appearanceSex ?? 0;
        var eth = appearanceEthnicity ?? 0;
        var first = ResolveFirstName(givenNameId, sex, eth);
        var fam = ResolveFamilyName(familyNameId, eth);
        var sexLabel = sex == 1 ? "F" : "M";
        return $"{first} {fam} (#{rosterId}, {sexLabel})";
    }

    public string ResolveFirstName(int givenNameId, int appearanceSex, int appearanceEthnicity) =>
        _packs.ResolveFirstName(givenNameId, appearanceSex, appearanceEthnicity);

    public string ResolveFamilyName(int familyNameId, int appearanceEthnicity) =>
        _packs.ResolveFamilyName(familyNameId, appearanceEthnicity);

    public bool TryResolveCityPack(string cityName, out float latitude, out float longitude, out string packLabel) =>
        _packs.TryResolveCityPack(cityName, out latitude, out longitude, out packLabel);
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

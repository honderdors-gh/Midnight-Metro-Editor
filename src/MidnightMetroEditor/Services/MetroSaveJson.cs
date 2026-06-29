using System.Text.Json;
using System.Text.Json.Serialization;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

/// <summary>Deserialize Midnight Metro game saves (Unity JsonUtility field layout).</summary>
public static class MetroSaveJson
{
    static readonly JsonSerializerOptions Options = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = false,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never
    };

    public static MetroSaveFile Deserialize(string json) =>
        JsonSerializer.Deserialize<MetroSaveFile>(json, Options)
        ?? throw new InvalidDataException("Game save JSON deserialized to null.");

    public static string Serialize(MetroSaveFile file) =>
        JsonSerializer.Serialize(file, Options);

    public static string SerializePretty(MetroSaveFile file)
    {
        var options = new JsonSerializerOptions(Options) { WriteIndented = true };
        return JsonSerializer.Serialize(file, options);
    }
}

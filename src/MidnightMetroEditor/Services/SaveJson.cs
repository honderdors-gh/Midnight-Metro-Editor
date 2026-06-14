using System.Text.Json;
using System.Text.Json.Serialization;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public static class SaveJson
{
    static readonly JsonSerializerOptions ReadOptions = new()
    {
        IncludeFields = true,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        NumberHandling = JsonNumberHandling.AllowReadingFromString
    };

    static readonly JsonSerializerOptions WriteOptions = new()
    {
        IncludeFields = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        WriteIndented = false
    };

    public static CitySimSaveFile Deserialize(string json) =>
        JsonSerializer.Deserialize<CitySimSaveFile>(json, ReadOptions)
        ?? throw new InvalidDataException("Save JSON deserialized to null.");

    public static string Serialize(CitySimSaveFile file) =>
        JsonSerializer.Serialize(file, WriteOptions);

    public static string SerializePretty(CitySimSaveFile file)
    {
        var options = new JsonSerializerOptions(WriteOptions) { WriteIndented = true };
        return JsonSerializer.Serialize(file, options);
    }
}

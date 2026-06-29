using System.Text.Json;
using System.Text.Json.Nodes;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

/// <summary>
/// Merge typed edits back into the original JSON so unknown/new game fields survive round-trip.
/// </summary>
public static class MetroSaveJsonMerge
{
    static readonly JsonSerializerOptions NodeWriteOptions = new() { WriteIndented = false };

    public static string MergeOriginalWithFile(string originalJson, MetroSaveFile file)
    {
        var original = JsonNode.Parse(originalJson)?.AsObject()
            ?? throw new InvalidDataException("Original save JSON root is not an object.");

        var updated = JsonNode.Parse(MetroSaveJson.Serialize(file))?.AsObject()
            ?? throw new InvalidDataException("Updated save JSON root is not an object.");

        var merged = MergeObjects(original, updated);
        return merged.ToJsonString(NodeWriteOptions);
    }

    static JsonObject MergeObjects(JsonObject original, JsonObject updated)
    {
        var result = CloneObject(updated);
        foreach (var (key, originalValue) in original)
        {
            if (originalValue == null)
                continue;

            if (!result.TryGetPropertyValue(key, out var updatedValue) || updatedValue == null)
            {
                result[key] = CloneNode(originalValue);
                continue;
            }

            if (originalValue is JsonObject originalObject && updatedValue is JsonObject updatedObject)
                result[key] = MergeObjects(originalObject, updatedObject);
        }

        return result;
    }

    static JsonObject CloneObject(JsonObject source) =>
        JsonNode.Parse(source.ToJsonString())!.AsObject();

    static JsonNode CloneNode(JsonNode source) =>
        JsonNode.Parse(source.ToJsonString())!;
}

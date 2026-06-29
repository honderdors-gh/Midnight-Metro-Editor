using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace MidnightMetroEditor.Services;

public sealed class MetroSaveVerifyResult
{
    public int Version { get; init; }
    public int Day { get; init; }
    public string? CityName { get; init; }
    public int ResidentCount { get; init; }
    public int DeceasedCount { get; init; }
    public int GridWidth { get; init; }
    public int GridHeight { get; init; }
    public int Treasury { get; init; }
    public bool MetroNetworkResetPending { get; init; }
}

/// <summary>Structural checks for Midnight Metro game saves without legacy re-serialize.</summary>
public static class MetroSaveVerify
{
    static readonly string[] RequiredResidentArrays =
    {
        "rosterId",
        "givenNameId",
        "familyNameId",
        "appearanceSex",
        "appearanceEthnicity",
        "homeX",
        "homeY",
        "workplaceX",
        "workplaceY",
        "job",
        "traitIntelligence",
        "traitHealth",
        "traitAggression"
    };

    public static MetroSaveVerifyResult ValidateGameSaveOrThrow(string json)
    {
        MetroSaveGameFormat.EnsureGameSaveOrThrow(json);

        var root = JsonNode.Parse(json)?.AsObject()
            ?? throw new InvalidDataException("Save JSON root is not an object.");

        var session = root["session"] as JsonObject
            ?? throw new InvalidDataException("Save JSON is missing 'session'.");

        var residents = root["residents"] as JsonObject
            ?? throw new InvalidDataException("Save JSON is missing 'residents'.");

        var roster = residents["rosterId"] as JsonArray
            ?? throw new InvalidDataException("residents.rosterId is missing or not an array.");

        var count = roster.Count;
        foreach (var field in RequiredResidentArrays)
            ValidateArrayLength(residents, field, count);

        var deceasedCount = 0;
        if (root["deceased"] is JsonObject deceased
            && deceased["rosterId"] is JsonArray deadRoster)
        {
            deceasedCount = deadRoster.Count;
            if (deceasedCount > 0)
            {
                foreach (var field in RequiredResidentArrays)
                    ValidateArrayLength(deceased, field, deceasedCount);
            }
        }

        var grid = root["grid"] as JsonObject;
        return new MetroSaveVerifyResult
        {
            Version = root["version"]?.GetValue<int>() ?? 0,
            Day = session["day"]?.GetValue<int>() ?? 0,
            CityName = session["cityName"]?.GetValue<string>(),
            ResidentCount = count,
            DeceasedCount = deceasedCount,
            GridWidth = grid?["width"]?.GetValue<int>() ?? 0,
            GridHeight = grid?["height"]?.GetValue<int>() ?? 0,
            Treasury = session["treasury"]?.GetValue<int>() ?? 0,
            MetroNetworkResetPending = session["metroNetworkResetPending"]?.GetValue<int>() == 1
        };
    }

    public static string Describe(MetroSaveVerifyResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"version={result.Version} day={result.Day} city={result.CityName ?? "?"}");
        sb.AppendLine($"residents={result.ResidentCount:N0} deceased={result.DeceasedCount:N0}");
        sb.AppendLine($"grid={result.GridWidth}x{result.GridHeight} treasury=${result.Treasury:N0}");
        if (result.MetroNetworkResetPending)
            sb.AppendLine("metroNetworkResetPending=1 (game will auto-expand metro on load)");
        return sb.ToString().TrimEnd();
    }

    static void ValidateArrayLength(JsonObject parent, string field, int expectedCount)
    {
        if (!parent.TryGetPropertyValue(field, out var node) || node is not JsonArray array)
            throw new InvalidDataException($"residents.{field} is missing or not an array.");

        if (array.Count != expectedCount)
        {
            throw new InvalidDataException(
                $"residents.{field} length {array.Count} != rosterId length {expectedCount}. " +
                "The save may be corrupted (often from opening a game save in the legacy editor and saving).");
        }
    }
}

using System.Text;
using System.Text.Json.Nodes;

namespace MidnightMetroEditor.Services;

/// <summary>Clears subway geometry and metro line catalog in a Midnight Metro game save (gzip JSON).</summary>
public static class MetroSaveRepair
{
    const string EmptySubwayJson =
        "{\"x\":[],\"y\":[],\"kind\":[],\"linkAx\":[],\"linkAy\":[],\"linkBx\":[],\"linkBy\":[]," +
        "\"stationDepthFlat\":[],\"linkADepth\":[],\"linkBDepth\":[],\"linkKind\":[]}";

    public static void ResetMetroNetwork(string path, bool createBackup = true)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Save path is required.", nameof(path));
        if (!File.Exists(path))
            throw new FileNotFoundException("Save file not found.", path);

        if (createBackup)
        {
            var backup = path + ".bak_metro_reset";
            File.Copy(path, backup, overwrite: true);
        }

        var json = SaveCompression.ReadSaveText(path);
        MetroSaveGameFormat.EnsureGameSaveOrThrow(json);

        var namingPack = ExtractMetroLinesString(json, "cityNamingPackId");
        var namingStyle = ExtractMetroLinesString(json, "cityNamingStyleId");

        json = JsonObjectPatch.ReplaceObject(json, "subway", EmptySubwayJson);
        json = JsonObjectPatch.ReplaceObject(
            json,
            "metroLines",
            "{\"nextLineId\":1," +
            $"\"cityNamingPackId\":\"{EscapeJson(namingPack)}\"," +
            $"\"cityNamingStyleId\":\"{EscapeJson(namingStyle)}\"," +
            "\"lines\":[],\"linkOwners\":[],\"stations\":[],\"buildJobs\":[]," +
            "\"dailyRiders\":0,\"pulseRiders\":0,\"ridershipByLine\":[]}");
        json = JsonObjectPatch.UpsertSessionInt(json, "metroNetworkResetPending", 1);

        MetroSaveVerify.ValidateGameSaveOrThrow(json);

        File.WriteAllBytes(path, SaveCompression.Encode(json));
    }

    public static string DescribeAfterReset(string json)
    {
        MetroSaveGameFormat.EnsureGameSaveOrThrow(json);
        var root = JsonNode.Parse(json)?.AsObject();
        var session = root?["session"] as JsonObject;
        var city = session?["cityName"]?.GetValue<string>() ?? "?";
        var day = session?["day"]?.GetValue<int>() ?? 0;
        var sb = new StringBuilder();
        sb.AppendLine($"City: {city} (day {day})");
        sb.AppendLine("Metro tunnels/stations: cleared");
        sb.AppendLine("Metro line catalog: cleared (naming pack kept)");
        sb.AppendLine("metroNetworkResetPending: set — game will auto-expand for 5 sim days on load");
        return sb.ToString();
    }

    static string ExtractMetroLinesString(string json, string field)
    {
        var mlIdx = json.IndexOf("\"metroLines\":", StringComparison.Ordinal);
        if (mlIdx < 0)
            return "";

        var needle = $"\"{field}\":\"";
        var idx = json.IndexOf(needle, mlIdx, StringComparison.Ordinal);
        if (idx < 0)
            return "";

        var start = idx + needle.Length;
        var end = json.IndexOf('"', start);
        return end > start ? json[start..end] : "";
    }

    static string EscapeJson(string value) =>
        value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}

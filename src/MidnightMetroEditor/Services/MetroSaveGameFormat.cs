namespace MidnightMetroEditor.Services;

/// <summary>Detect Midnight Metro game saves (Unity JsonUtility) vs legacy editor schema.</summary>
public static class MetroSaveGameFormat
{
    public static bool IsGameSave(string json) =>
        !string.IsNullOrEmpty(json)
        && json.Contains("\"grid\":{\"width\"", StringComparison.Ordinal)
        && json.Contains("\"residents\":{", StringComparison.Ordinal)
        && json.Contains("\"homeX\":", StringComparison.Ordinal);

    public static void EnsureGameSaveOrThrow(string json)
    {
        if (!IsGameSave(json))
            throw new InvalidDataException(
                "Not a Midnight Metro game save (expected grid.width + residents.homeX). " +
                "The file may be corrupted or is a legacy editor prototype save.");
    }
}

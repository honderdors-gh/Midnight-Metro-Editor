using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public sealed class MetroGridCellView
{
    readonly MetroSaveGrid _grid;
    readonly int _index;

    public MetroGridCellView(MetroSaveGrid grid, int x, int y)
    {
        _grid = grid;
        X = x;
        Y = y;
        _index = y * grid.width + x;
    }

    public int X { get; }
    public int Y { get; }

    public int type { get => _grid.type![_index]; set => _grid.type![_index] = value; }
    public int zone { get => _grid.zone![_index]; set => _grid.zone![_index] = value; }
    public int population { get => _grid.population![_index]; set => _grid.population![_index] = value; }
    public int wealth { get => _grid.wealth![_index]; set => _grid.wealth![_index] = value; }
    public int ownerRosterId { get => _grid.ownerRosterId![_index]; set => _grid.ownerRosterId![_index] = value; }
    public int lotWidth { get => _grid.lotWidth![_index]; set => _grid.lotWidth![_index] = value; }
    public int lotHeight { get => _grid.lotHeight![_index]; set => _grid.lotHeight![_index] = value; }
    public int maxFloors { get => _grid.maxFloors![_index]; set => _grid.maxFloors![_index] = value; }
    public int builtFloors { get => _grid.builtFloors![_index]; set => _grid.builtFloors![_index] = value; }
    public int densityTier { get => _grid.densityTier![_index]; set => _grid.densityTier![_index] = value; }
    public int jobSlotTarget { get => _grid.jobSlotTarget![_index]; set => _grid.jobSlotTarget![_index] = value; }
    public string? buildingItemId { get => _grid.buildingItemId![_index]; set => _grid.buildingItemId![_index] = value; }

    public string Summary =>
        $"({X},{Y}) type={type} zone={zone} pop={population} wealth={wealth} owner=#{ownerRosterId}";
}

public static class MetroGameGridHelper
{
    public static bool TryGetCellView(MetroSaveFile file, int x, int y, out MetroGridCellView? view)
    {
        view = null;
        var grid = file.grid;
        if (grid.width <= 0 || grid.height <= 0)
            return false;
        if (x < 0 || y < 0 || x >= grid.width || y >= grid.height)
            return false;
        if (grid.type == null || grid.zone == null)
            return false;

        view = new MetroGridCellView(grid, x, y);
        return true;
    }

    public static string BuildOverview(MetroSaveFile file)
    {
        var session = file.session;
        var residents = file.residents.rosterId?.Length ?? 0;
        var deceased = file.deceased.rosterId?.Length ?? 0;
        var grid = file.grid;
        var subwayCells = file.subway.x?.Length ?? 0;
        var metroLines = file.metroLines.lines?.Length ?? 0;
        var newsArticles = file.news.articles?.Length ?? 0;

        var versionNote = file.version > MetroSaveSchema.CurrentSaveVersion
            ? $"\r\n⚠ Save version {file.version} is newer than editor schema v{MetroSaveSchema.CurrentSaveVersion} — run scripts/sync-save-schema.ps1\r\n"
            : "";

        return
            $"Midnight Metro game save\r\n" +
            versionNote +
            $"Version: {file.version} (editor supports through v{MetroSaveSchema.CurrentSaveVersion})\r\n" +
            $"Saved: {file.savedUtc}\r\n" +
            $"City: {session.cityName}\r\n" +
            $"Day: {session.day}\r\n" +
            $"Seed: {session.randomSeed}\r\n" +
            $"Grid: {grid.width} x {grid.height}\r\n" +
            $"Treasury: ${session.treasury:N0}\r\n" +
            $"Residents: {residents:N0}\r\n" +
            $"Deceased archive: {deceased:N0}\r\n" +
            $"Subway cells: {subwayCells}\r\n" +
            $"Metro lines: {metroLines}\r\n" +
            $"News articles: {newsArticles}\r\n" +
            $"Mayor roster id: {file.elections.mayorRosterId}\r\n" +
            (session.metroNetworkResetPending != 0
                ? "metroNetworkResetPending: yes\r\n"
                : "");
    }
}

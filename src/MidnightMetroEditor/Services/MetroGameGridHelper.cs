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
        IsLotAnchor = MetroGridLotHelper.IsLotAnchor(grid, x, y);
    }

    public int X { get; }
    public int Y { get; }
    public bool IsLotAnchor { get; }

    public int type { get => Read(_grid.type); set => Write(_grid.type, value); }
    public int zone { get => Read(_grid.zone); set => Write(_grid.zone, value); }
    public int population { get => Read(_grid.population); set => Write(_grid.population, value); }
    public int wealth { get => Read(_grid.wealth); set => Write(_grid.wealth, value); }
    public int ownerRosterId { get => Read(_grid.ownerRosterId); set => Write(_grid.ownerRosterId, value); }

    public int lotWidth { get => Read(_grid.lotWidth); set => Write(_grid.lotWidth, value); }
    public int lotHeight { get => Read(_grid.lotHeight); set => Write(_grid.lotHeight, value); }
    public int lotAnchorX { get => Read(_grid.lotAnchorX); set => Write(_grid.lotAnchorX, value); }
    public int lotAnchorY { get => Read(_grid.lotAnchorY); set => Write(_grid.lotAnchorY, value); }
    public int maxFloors { get => Read(_grid.maxFloors); set => Write(_grid.maxFloors, value); }
    public int builtFloors { get => Read(_grid.builtFloors); set => Write(_grid.builtFloors, value); }
    public int densityTier { get => Read(_grid.densityTier); set => Write(_grid.densityTier, value); }

    public string? buildingItemId { get => ReadString(_grid.buildingItemId); set => WriteString(_grid.buildingItemId, value); }
    public string? rooftopItemId { get => ReadString(_grid.rooftopItemId); set => WriteString(_grid.rooftopItemId, value); }
    public int embeddedCivicType { get => Read(_grid.embeddedCivicType); set => Write(_grid.embeddedCivicType, value); }

    public int chunkUnlocked { get => Read(_grid.chunkUnlocked); set => Write(_grid.chunkUnlocked, value); }

    public int pendingLotWidth { get => Read(_grid.pendingLotWidth); set => Write(_grid.pendingLotWidth, value); }
    public int pendingLotHeight { get => Read(_grid.pendingLotHeight); set => Write(_grid.pendingLotHeight, value); }
    public int constructionKind { get => Read(_grid.constructionKind); set => Write(_grid.constructionKind, value); }
    public int constructionCompleteDay { get => Read(_grid.constructionCompleteDay); set => Write(_grid.constructionCompleteDay, value); }

    public int lotKind { get => Read(_grid.lotKind); set => Write(_grid.lotKind, value); }
    public int roadSubtype { get => Read(_grid.roadSubtype); set => Write(_grid.roadSubtype, value); }
    public int wayGrade { get => Read(_grid.wayGrade); set => Write(_grid.wayGrade, value); }
    public int headingDeg { get => Read(_grid.headingDeg); set => Write(_grid.headingDeg, value); }
    public int roadHalfMask { get => Read(_grid.roadHalfMask); set => Write(_grid.roadHalfMask, value); }
    public int trackSubtype { get => Read(_grid.trackSubtype); set => Write(_grid.trackSubtype, value); }
    public int speedLimitKmh { get => Read(_grid.speedLimitKmh); set => Write(_grid.speedLimitKmh, value); }

    public string Summary
    {
        get
        {
            var anchor = IsLotAnchor ? " [anchor]" : "";
            return $"({X},{Y}){anchor} type={type} zone={zone} lotKind={lotKind} road={roadSubtype}/{wayGrade} lot={lotWidth}x{lotHeight}@{lotAnchorX},{lotAnchorY} pop={population}";
        }
    }

    int Read(int[]? array) => array != null && _index < array.Length ? array[_index] : 0;

    void Write(int[]? array, int value)
    {
        if (array != null && _index < array.Length)
            array[_index] = value;
    }

    string? ReadString(string[]? array) =>
        array != null && _index < array.Length ? array[_index] : null;

    void WriteString(string[]? array, string? value)
    {
        if (array != null && _index < array.Length)
            array[_index] = value;
    }
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

    public static string BuildOverview(MetroSaveFile file, NameDatabase? names = null)
    {
        var session = file.session;
        var residents = file.residents.rosterId?.Length ?? 0;
        var deceased = file.deceased.rosterId?.Length ?? 0;
        var grid = file.grid;
        var subwayCells = file.subway.x?.Length ?? 0;
        var metroLines = file.metroLines.lines?.Length ?? 0;
        var newsArticles = file.news.articles?.Length ?? 0;
        var gangCount = file.gangs.gangId?.Length ?? 0;
        var ledgerRows = file.criminalLedger.rosterId?.Length ?? 0;
        var justiceCases = file.justice.cases?.Length ?? 0;
        var workplaces = file.workplaces.workplaceId?.Length ?? 0;
        var lots = MetroLotIndex.Build(file).Count;

        var versionNote = file.version > MetroSaveSchema.CurrentSaveVersion
            ? $"\r\n⚠ Save version {file.version} is newer than editor schema v{MetroSaveSchema.CurrentSaveVersion} — run scripts/sync-save-schema.ps1\r\n"
            : "";

        var geoLine = $"Latitude: {session.latitude:F4}°  Longitude: {session.longitude:F4}°\r\n";
        if (names != null && names.TryResolveCityPack(session.cityName, out var packLat, out var packLon, out var packLabel))
            geoLine += $"City pack match: {packLabel} (center {packLat:F2}°, {packLon:F2}°)\r\n";
        geoLine += $"Weather kind: {session.weatherKind}  Temp: {session.temperatureC:0.#}°C  Speed: {session.speedPreset}\r\n";
        geoLine += $"World lot size: {session.worldMapLotSize}  Blocks/axis: {session.worldMapBlocksAxis}\r\n";

        var workplaceLine = file.version >= MetroWorkplaceStorage.WorkplaceSaveVersion
            ? $"Lots: {lots:N0}\r\nWorkplaces: {workplaces:N0}\r\n"
            : $"Lots: {lots:N0}\r\nWorkplace storage: {MetroWorkplaceStorage.LayerDescription(file.version)}\r\n";

        var namesLine = names != null
            ? $"Name pools: {names.Packs.SourceSummary}\r\n"
            : "";

        return
            $"Midnight Metro game save\r\n" +
            versionNote +
            $"Version: {file.version} (editor supports through v{MetroSaveSchema.CurrentSaveVersion})\r\n" +
            $"Saved: {file.savedUtc}\r\n" +
            $"City: {session.cityName}\r\n" +
            geoLine +
            namesLine +
            $"Day: {session.day}\r\n" +
            $"Seed: {session.randomSeed}\r\n" +
            $"Grid: {grid.width} x {grid.height}\r\n" +
            $"Treasury: ${session.treasury:N0}\r\n" +
            $"Residents: {residents:N0}\r\n" +
            $"Deceased archive: {deceased:N0}\r\n" +
            workplaceLine +
            $"Subway cells: {subwayCells}\r\n" +
            $"Metro lines: {metroLines}\r\n" +
            $"News articles: {newsArticles}\r\n" +
            $"Gangs: {gangCount} · criminal ledger: {ledgerRows} · justice cases: {justiceCases}\r\n" +
            CivicOverviewLine(file, names) +
            (session.metroNetworkResetPending != 0
                ? "metroNetworkResetPending: yes\r\n"
                : "");
    }

    static string CivicOverviewLine(MetroSaveFile file, NameDatabase? names)
    {
        var e = file.elections;
        var nameDb = names ?? new NameDatabase();
        var civic = MetroCivicOfficeQuery.Scan(file, nameDb);

        int Count(string office) => civic.Count(r => r.Office == office);
        return
            $"Mayor: {FormatVacant(e.mayorRosterId)} · Council: {Count("Council member")} · Judges: {Count("Judge")} · " +
            $"DA: {FormatVacant(e.districtAttorneyRosterId)} · Prosecutors: {Count("Prosecutor")} · " +
            $"Police chief: {FormatVacant(e.policeChiefRosterId)}\r\n" +
            $"Elections — mayor day {e.nextMayorElectionDay} · council day {e.nextCouncilElectionDay} · " +
            $"active race {e.activeRace} · campaign ends day {e.electionDay}\r\n";
    }

    static string FormatVacant(int rosterId) => rosterId > 0 ? $"#{rosterId}" : "vacant";
}

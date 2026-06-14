using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public sealed class GridCellView
{
    readonly SaveGridBulk _grid;
    readonly int _index;

    public GridCellView(SaveGridBulk grid, int x, int y)
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
    public float crimeLevel { get => _grid.crimeLevel![_index]; set => _grid.crimeLevel![_index] = value; }
    public float policeInfluence { get => _grid.policeInfluence![_index]; set => _grid.policeInfluence![_index] = value; }
    public float patrolBoost { get => _grid.patrolBoost![_index]; set => _grid.patrolBoost![_index] = value; }
    public float fireInfluence { get => _grid.fireInfluence![_index]; set => _grid.fireInfluence![_index] = value; }
    public float medicalInfluence { get => _grid.medicalInfluence![_index]; set => _grid.medicalInfluence![_index] = value; }
    public int population { get => _grid.population![_index]; set => _grid.population![_index] = value; }
    public int wealth { get => _grid.wealth![_index]; set => _grid.wealth![_index] = value; }
    public int ownerRosterId { get => _grid.ownerRosterId![_index]; set => _grid.ownerRosterId![_index] = value; }
    public int activeCrimeTier { get => _grid.activeCrimeTier![_index]; set => _grid.activeCrimeTier![_index] = value; }
    public float crimeTimerHours { get => _grid.crimeTimerHours![_index]; set => _grid.crimeTimerHours![_index] = value; }
    public int gangId { get => _grid.gangId![_index]; set => _grid.gangId![_index] = value; }
    public int fireCallTier { get => _grid.fireCallTier![_index]; set => _grid.fireCallTier![_index] = value; }
    public int medicalCallTier { get => _grid.medicalCallTier![_index]; set => _grid.medicalCallTier![_index] = value; }
    public float businessHealth { get => _grid.businessHealth![_index]; set => _grid.businessHealth![_index] = value; }
    public int businessClosed { get => _grid.businessClosed![_index]; set => _grid.businessClosed![_index] = value; }
    public int jailedSuspectRosterId { get => _grid.jailedSuspectRosterId![_index]; set => _grid.jailedSuspectRosterId![_index] = value; }
    public float jailHoursRemaining { get => _grid.jailHoursRemaining![_index]; set => _grid.jailHoursRemaining![_index] = value; }
    public int lotMixTier { get => _grid.lotMixTier![_index]; set => _grid.lotMixTier![_index] = value; }
    public int lotMixTrack { get => _grid.lotMixTrack![_index]; set => _grid.lotMixTrack![_index] = value; }

    public string Summary =>
        $"({X},{Y}) {OccupationLabels.CellTypeLabel(type)} / {OccupationLabels.ZoneLabel(zone)} pop={population} gang={gangId}";
}

public static class GridHelper
{
    public static bool TryGetCellView(CitySimSaveFile file, int x, int y, out GridCellView? view)
    {
        view = null;
        var grid = file.grid;
        if (grid == null || grid.width <= 0 || grid.height <= 0)
            return false;
        if (x < 0 || y < 0 || x >= grid.width || y >= grid.height)
            return false;
        if (grid.type == null || grid.zone == null)
            return false;

        view = new GridCellView(grid, x, y);
        return true;
    }

    public static string BuildOverview(CitySimSaveFile file)
    {
        var session = file.session;
        var residents = file.residents?.rosterId?.Length ?? 0;
        var agents = file.agents.Count;
        var gangs = file.gangs?.gangs.Count ?? 0;
        var cases = file.criminalCases.Count;
        var grid = file.grid;

        return
            $"Version: {file.version}\r\n" +
            $"Saved: {file.savedUtc}\r\n" +
            $"City: {session?.cityName ?? "(unnamed)"}\r\n" +
            $"Day: {session?.day ?? 0}\r\n" +
            $"Seed: {session?.randomSeed ?? 0}\r\n" +
            $"Grid: {grid?.width ?? session?.gridWidth ?? 0} x {grid?.height ?? session?.gridHeight ?? 0}\r\n" +
            $"Treasury: ${file.budget?.balance ?? 0:N0}\r\n" +
            $"Residents: {residents}\r\n" +
            $"Agents: {agents}\r\n" +
            $"Gangs: {gangs}\r\n" +
            $"Criminal cases: {cases}\r\n" +
            $"Honor wall: {file.honorWall.Count}\r\n" +
            $"Metrics days: {file.metricsHistory.Count}\r\n" +
            $"Player actions remaining: {file.playerAgency?.actionsRemaining ?? 0}/{file.playerAgency?.actionsPerDay ?? 0}";
    }
}

public sealed class SaveDocument
{
    public CitySimSaveFile File { get; private set; } = new();
    public string? Path { get; private set; }
    public bool IsDirty { get; set; }
    public NameDatabase Names { get; } = new();

    public void Load(string path)
    {
        var json = SaveCompression.ReadSaveText(path);
        File = SaveJson.Deserialize(json);
        Path = path;
        IsDirty = false;
        RefreshNames();

        var settings = EditorSettings.Load();
        settings.LastSaveDirectory = System.IO.Path.GetDirectoryName(path);
        EditorSettings.Save(settings);
    }

    public void Save(string? path = null)
    {
        path ??= Path ?? throw new InvalidOperationException("No save path.");
        SaveCompression.WriteSave(path, File);
        Path = path;
        IsDirty = false;
    }

    public void Reload()
    {
        if (Path == null)
            throw new InvalidOperationException("No file loaded.");
        Load(Path);
    }

    public void ReplaceFile(CitySimSaveFile file, string? path = null)
    {
        File = file;
        if (path != null)
            Path = path;
        IsDirty = true;
    }

    public void ImportFromJson(string json)
    {
        File = SaveJson.Deserialize(json);
        Path = null;
        IsDirty = true;
    }

    public void RefreshNames() => Names.Load(NameDatabase.ResolveGameNamesPath());

    public string Title =>
        (Path != null ? System.IO.Path.GetFileName(Path) : "Untitled") + (IsDirty ? " *" : "");
}

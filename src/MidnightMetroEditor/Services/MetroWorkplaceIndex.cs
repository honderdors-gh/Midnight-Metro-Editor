using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

/// <summary>
/// Workplace data authority across save versions (grid job arrays, nightlife vice, workplaces table).
/// </summary>
public static class MetroWorkplaceStorage
{
    public const int WorkplaceSaveVersion = 56;

    public enum StorageLayer
    {
        /// <summary>v55+ — <see cref="MetroSaveWorkplaces"/> is authoritative.</summary>
        WorkplacesTable,
        /// <summary>v26–54 — per-cell grid job arrays at lot anchors.</summary>
        GridJobArrays,
        /// <summary>v51–54 — vice rows in <see cref="MetroSaveNightlife"/> (merged into workplaces on load).</summary>
        NightlifeVice
    }

    public static StorageLayer PrimaryLayer(int saveVersion) =>
        saveVersion >= WorkplaceSaveVersion
            ? StorageLayer.WorkplacesTable
            : StorageLayer.GridJobArrays;

    public static string LayerDescription(int saveVersion)
    {
        if (saveVersion >= WorkplaceSaveVersion)
            return "v55+ workplaces table (wealth per workplace v56+; grid job arrays are not written)";

        var parts = new List<string> { "v26–54 grid job arrays at lot anchors" };
        if (saveVersion >= 51)
            parts.Add("v51–54 nightlife vice rows");
        return string.Join("; ", parts);
    }

}

public sealed class WorkplaceListRow
{
    public int RowIndex { get; init; }
    public int WorkplaceId { get; init; }
    public int AnchorX { get; init; }
    public int AnchorY { get; init; }
    public string SiteKind { get; init; } = "";
    public string Category { get; init; } = "";
    public int JobSlots { get; init; }
    public int ViceKind { get; init; }
    public int Wealth { get; init; }
    public string StorageLayer { get; init; } = "";
    public object EditTarget { get; init; } = null!;
}

public sealed class LotWorkplaceRef
{
    public string Label { get; init; } = "";
    public int WorkplaceId { get; init; }
    public int AnchorX { get; init; }
    public int AnchorY { get; init; }
    public int JobSlots { get; init; }
    public object EditTarget { get; init; } = null!;
}

public static class MetroWorkplaceIndex
{
    public static List<EditorTreeItem> BuildTree(MetroSaveFile file, NameDatabase names)
    {
        var roots = new List<EditorTreeItem>();
        foreach (var row in Build(file))
            roots.Add(BuildWorkplaceNode(file, row, names));
        return roots;
    }

    static EditorTreeItem BuildWorkplaceNode(MetroSaveFile file, WorkplaceListRow row, NameDatabase names)
    {
        var anchorLabel = $"{row.AnchorX},{row.AnchorY}";
        var idLabel = row.WorkplaceId > 0 ? $"#{row.WorkplaceId}" : $"grid {anchorLabel}";
        var root = new EditorTreeItem
        {
            Label = $"{idLabel} {row.SiteKind} · {row.Category} · {row.JobSlots} slot{(row.JobSlots == 1 ? "" : "s")}"
                + (row.Wealth > 0 ? $" · W{row.Wealth}" : ""),
            EditTarget = row.EditTarget,
            Children = BuildJobSlotChildren(file, row, names)
        };
        return root;
    }

    public static List<EditorTreeItem> BuildJobSlotChildren(MetroSaveFile file, WorkplaceListRow row, NameDatabase names)
    {
        var children = new List<EditorTreeItem>();
        var staff = MetroCitizenSaveQueries.FindStaffAtWorkplace(file, row.WorkplaceId, row.AnchorX, row.AnchorY);
        var slotCount = Math.Max(row.JobSlots, staff.Count > 0 ? staff.Max(s => s.SlotIndex) + 1 : 0);

        if (slotCount <= 0 && staff.Count == 0)
        {
            children.Add(new EditorTreeItem { Label = "(no job slots)" });
            return children;
        }

        if (slotCount <= 0)
            slotCount = staff.Count;

        for (var slot = 0; slot < slotCount; slot++)
        {
            var worker = staff.FirstOrDefault(s => s.SlotIndex == slot && !s.IsSubwaySlot);
            var slotEditor = new MetroWorkplaceJobSlotEditor(
                file,
                names,
                row.WorkplaceId,
                row.AnchorX,
                row.AnchorY,
                slot,
                worker?.ResidentIndex ?? -1);

            children.Add(new EditorTreeItem
            {
                Label = slotEditor.SummaryLabel,
                EditTarget = slotEditor
            });
        }

        foreach (var extra in staff.Where(s => s.SlotIndex < 0 || s.SlotIndex >= slotCount))
        {
            var slotEditor = new MetroWorkplaceJobSlotEditor(
                file,
                names,
                row.WorkplaceId,
                row.AnchorX,
                row.AnchorY,
                extra.SlotIndex,
                extra.ResidentIndex);

            children.Add(new EditorTreeItem
            {
                Label = $"{slotEditor.SummaryLabel} (unassigned slot)",
                EditTarget = slotEditor
            });
        }

        return children;
    }

    /// <summary>Workplaces bound to a lot anchor (lot site + household sites).</summary>
    public static List<LotWorkplaceRef> GetWorkplacesAtLot(MetroSaveFile file, int anchorX, int anchorY)
    {
        var results = new List<LotWorkplaceRef>();

        if (file.version >= MetroWorkplaceStorage.WorkplaceSaveVersion)
        {
            var wp = file.workplaces;
            if (wp.workplaceId != null)
            {
                for (var i = 0; i < wp.workplaceId.Length; i++)
                {
                    if (ReadInt(wp.anchorX, i) != anchorX || ReadInt(wp.anchorY, i) != anchorY)
                        continue;

                    var siteKind = ReadInt(wp.siteKind, i);
                    var category = WorkplaceKindLabels.Category(ReadInt(wp.category, i));
                    var id = ReadInt(wp.workplaceId, i);
                    var host = ReadInt(wp.hostRosterId, i);
                    var hostNote = siteKind == 1 && host > 0 ? $" host #{host}" : "";
                    results.Add(new LotWorkplaceRef
                    {
                        Label = $"#{id} {WorkplaceKindLabels.SiteKind(siteKind)} · {category}{hostNote}",
                        WorkplaceId = id,
                        AnchorX = anchorX,
                        AnchorY = anchorY,
                        JobSlots = ReadInt(wp.jobSlotTarget, i),
                        EditTarget = new MetroWorkplaceEditor(file, i)
                    });
                }
            }
        }
        else
        {
            var idx = MetroGridLotHelper.GridIndex(file.grid, anchorX, anchorY);
            var jobSlots = ReadInt(file.grid.jobSlotTarget, idx);
            var vacancy = ReadInt(file.grid.vacancyStreakDays, idx);
            var supply = ReadInt(file.grid.supplyFulfillment, idx);
            var customer = ReadInt(file.grid.customerFulfillment, idx);
            if (jobSlots > 0 || vacancy > 0 || supply > 0 || customer > 0)
            {
                results.Add(new LotWorkplaceRef
                {
                    Label = $"Grid jobs · {jobSlots} slot{(jobSlots == 1 ? "" : "s")}",
                    WorkplaceId = 0,
                    AnchorX = anchorX,
                    AnchorY = anchorY,
                    JobSlots = jobSlots,
                    EditTarget = new MetroGridJobEditor(file, anchorX, anchorY)
                });
            }

            if (file.version >= 51 && file.nightlife.lotX != null)
            {
                for (var i = 0; i < file.nightlife.lotX.Length; i++)
                {
                    if (ReadInt(file.nightlife.lotX, i) != anchorX || ReadInt(file.nightlife.lotY, i) != anchorY)
                        continue;

                    results.Add(new LotWorkplaceRef
                    {
                        Label = $"Nightlife vice · kind {ReadInt(file.nightlife.viceKind, i)}",
                        WorkplaceId = 0,
                        AnchorX = anchorX,
                        AnchorY = anchorY,
                        JobSlots = 0,
                        EditTarget = new MetroNightlifeEditor(file, i)
                    });
                }
            }
        }

        return results;
    }

    public static List<WorkplaceListRow> Build(MetroSaveFile file)
    {
        if (file.version >= MetroWorkplaceStorage.WorkplaceSaveVersion)
            return BuildFromWorkplacesTable(file);

        var rows = BuildFromLegacyGrid(file);
        if (file.version >= 51)
            rows.AddRange(BuildFromNightlife(file));
        return rows;
    }

    static List<WorkplaceListRow> BuildFromWorkplacesTable(MetroSaveFile file)
    {
        var rows = new List<WorkplaceListRow>();
        var wp = file.workplaces;
        if (wp.workplaceId == null)
            return rows;

        for (var i = 0; i < wp.workplaceId.Length; i++)
        {
            rows.Add(new WorkplaceListRow
            {
                RowIndex = i,
                WorkplaceId = ReadInt(wp.workplaceId, i),
                AnchorX = ReadInt(wp.anchorX, i),
                AnchorY = ReadInt(wp.anchorY, i),
                SiteKind = WorkplaceKindLabels.SiteKind(ReadInt(wp.siteKind, i)),
                Category = WorkplaceKindLabels.Category(ReadInt(wp.category, i)),
                JobSlots = ReadInt(wp.jobSlotTarget, i),
                ViceKind = ReadInt(wp.viceKind, i),
                Wealth = ReadInt(wp.wealth, i),
                StorageLayer = "workplaces",
                EditTarget = new MetroWorkplaceEditor(file, i)
            });
        }

        return rows;
    }

    static List<WorkplaceListRow> BuildFromLegacyGrid(MetroSaveFile file)
    {
        var rows = new List<WorkplaceListRow>();
        var grid = file.grid;
        if (grid.width <= 0 || grid.height <= 0 || grid.type == null)
            return rows;

        for (var y = 0; y < grid.height; y++)
        for (var x = 0; x < grid.width; x++)
        {
            if (!MetroGridLotHelper.IsLotAnchor(grid, x, y))
                continue;

            var idx = MetroGridLotHelper.GridIndex(grid, x, y);
            var jobSlots = ReadInt(grid.jobSlotTarget, idx);
            var vacancy = ReadInt(grid.vacancyStreakDays, idx);
            var supply = ReadInt(grid.supplyFulfillment, idx);
            var customer = ReadInt(grid.customerFulfillment, idx);
            if (jobSlots <= 0 && vacancy <= 0 && supply <= 0 && customer <= 0)
                continue;

            rows.Add(new WorkplaceListRow
            {
                RowIndex = idx,
                WorkplaceId = 0,
                AnchorX = x,
                AnchorY = y,
                SiteKind = "Lot",
                Category = WorkplaceKindLabels.CategoryFromZone(ReadInt(grid.zone, idx)),
                JobSlots = jobSlots,
                ViceKind = 0,
                StorageLayer = "grid",
                EditTarget = new MetroGridJobEditor(file, x, y)
            });
        }

        return rows;
    }

    static List<WorkplaceListRow> BuildFromNightlife(MetroSaveFile file)
    {
        var rows = new List<WorkplaceListRow>();
        var nl = file.nightlife;
        if (nl.lotX == null)
            return rows;

        for (var i = 0; i < nl.lotX.Length; i++)
        {
            rows.Add(new WorkplaceListRow
            {
                RowIndex = i,
                WorkplaceId = 0,
                AnchorX = ReadInt(nl.lotX, i),
                AnchorY = ReadInt(nl.lotY, i),
                SiteKind = "Lot",
                Category = "Nightlife",
                JobSlots = 0,
                ViceKind = ReadInt(nl.viceKind, i),
                StorageLayer = "nightlife",
                EditTarget = new MetroNightlifeEditor(file, i)
            });
        }

        return rows;
    }

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;
}

/// <summary>One staffed job slot at a workplace — edits citizen workplace binding.</summary>
public sealed class MetroWorkplaceJobSlotEditor
{
    readonly MetroSaveFile _file;
    readonly NameDatabase _names;
    readonly int _workplaceId;
    readonly int _anchorX;
    readonly int _anchorY;
    readonly int _slotIndex;
    int _residentIndex;

    public MetroWorkplaceJobSlotEditor(
        MetroSaveFile file,
        NameDatabase names,
        int workplaceId,
        int anchorX,
        int anchorY,
        int slotIndex,
        int residentIndex)
    {
        _file = file;
        _names = names;
        _workplaceId = workplaceId;
        _anchorX = anchorX;
        _anchorY = anchorY;
        _slotIndex = slotIndex;
        _residentIndex = residentIndex;
    }

    public int slotIndex => _slotIndex;
    public int workplaceId => _workplaceId;
    public int anchorX => _anchorX;
    public int anchorY => _anchorY;

    public int rosterId
    {
        get => _residentIndex >= 0 ? ReadInt(_file.residents.rosterId, _residentIndex) : 0;
        set
        {
            ClearSlotOccupant();
            _residentIndex = -1;
            if (value <= 0)
                return;

            var idx = MetroCitizenSaveQueries.FindResidentIndexByRosterId(_file.residents, value);
            if (idx < 0)
                return;

            WriteInt(_file.residents.workplaceId, idx, _workplaceId);
            WriteInt(_file.residents.workplaceX, idx, _anchorX);
            WriteInt(_file.residents.workplaceY, idx, _anchorY);
            WriteInt(_file.residents.workSlotIndex, idx, _slotIndex);
            WriteInt(_file.residents.workSubwaySlot, idx, 0);
            if (ReadInt(_file.residents.job, idx) == 0)
                WriteInt(_file.residents.job, idx, 1);
            _residentIndex = idx;
        }
    }

    public int job
    {
        get => _residentIndex >= 0 ? ReadInt(_file.residents.job, _residentIndex) : 0;
        set
        {
            if (_residentIndex < 0)
                return;
            WriteInt(_file.residents.job, _residentIndex, value);
        }
    }

    public string JobLabel =>
        _residentIndex >= 0
            ? ResidentOccupationLabels.Format(_file.residents, _residentIndex, _file.grid, CitizenSourceKind.Resident)
            : "(vacant)";

    public string ResolvedName =>
        _residentIndex >= 0
            ? MetroCitizenSaveQueries.FormatResidentName(_file.residents, _residentIndex, _names)
            : "(vacant)";

    public string SummaryLabel =>
        rosterId > 0
            ? $"Slot {_slotIndex} · {JobLabel} · #{rosterId}"
            : $"Slot {_slotIndex} · (vacant)";

    void ClearSlotOccupant()
    {
        var staff = MetroCitizenSaveQueries.FindStaffAtWorkplace(_file, _workplaceId, _anchorX, _anchorY);
        foreach (var worker in staff)
        {
            if (worker.SlotIndex != _slotIndex || worker.IsSubwaySlot)
                continue;

            WriteInt(_file.residents.workplaceId, worker.ResidentIndex, 0);
            WriteInt(_file.residents.job, worker.ResidentIndex, 0);
            WriteInt(_file.residents.workSlotIndex, worker.ResidentIndex, -1);
            if (_residentIndex == worker.ResidentIndex)
                _residentIndex = -1;
        }
    }

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;

    static void WriteInt(int[]? array, int index, int value)
    {
        if (array != null && index >= 0 && index < array.Length)
            array[index] = value;
    }
}

public sealed class MetroWorkplaceEditor
{
    readonly MetroSaveFile _file;
    readonly int _index;

    public MetroWorkplaceEditor(MetroSaveFile file, int index)
    {
        _file = file;
        _index = index;
    }

    public string StorageNote => "Authoritative at save v55+ (workplaces table)";

    public int workplaceId { get => Read(_file.workplaces.workplaceId); set => Write(_file.workplaces.workplaceId, value); }
    public int anchorX { get => Read(_file.workplaces.anchorX); set => Write(_file.workplaces.anchorX, value); }
    public int anchorY { get => Read(_file.workplaces.anchorY); set => Write(_file.workplaces.anchorY, value); }
    public int siteKind { get => Read(_file.workplaces.siteKind); set => Write(_file.workplaces.siteKind, value); }
    public string SiteKindLabel => WorkplaceKindLabels.SiteKind(siteKind);
    public int category { get => Read(_file.workplaces.category); set => Write(_file.workplaces.category, value); }
    public string CategoryLabel => WorkplaceKindLabels.Category(category);
    public int hostRosterId { get => Read(_file.workplaces.hostRosterId); set => Write(_file.workplaces.hostRosterId, value); }
    public int jobSlotTarget { get => Read(_file.workplaces.jobSlotTarget); set => Write(_file.workplaces.jobSlotTarget, value); }
    public int vacancyStreakDays { get => Read(_file.workplaces.vacancyStreakDays); set => Write(_file.workplaces.vacancyStreakDays, value); }
    public int supplyFulfillment { get => Read(_file.workplaces.supplyFulfillment); set => Write(_file.workplaces.supplyFulfillment, value); }
    public int customerFulfillment { get => Read(_file.workplaces.customerFulfillment); set => Write(_file.workplaces.customerFulfillment, value); }
    public int viceKind { get => Read(_file.workplaces.viceKind); set => Write(_file.workplaces.viceKind, value); }
    public int ownerGangId { get => Read(_file.workplaces.ownerGangId); set => Write(_file.workplaces.ownerGangId, value); }
    public int wealth { get => Read(_file.workplaces.wealth); set => Write(_file.workplaces.wealth, value); }

    int Read(int[]? array) => array != null && _index < array.Length ? array[_index] : 0;

    void Write(int[]? array, int value)
    {
        if (array != null && _index < array.Length)
            array[_index] = value;
    }
}

/// <summary>Legacy grid job data at a lot anchor (v26–54).</summary>
public sealed class MetroGridJobEditor
{
    readonly MetroSaveFile _file;
    readonly int _index;

    public MetroGridJobEditor(MetroSaveFile file, int x, int y)
    {
        _file = file;
        _index = MetroGridLotHelper.GridIndex(file.grid, x, y);
        AnchorX = x;
        AnchorY = y;
    }

    public int AnchorX { get; }
    public int AnchorY { get; }
    public string StorageNote => "Legacy v26–54 — job data stored on grid arrays at lot anchor";

    public int jobSlotTarget { get => Read(_file.grid.jobSlotTarget); set => Write(_file.grid.jobSlotTarget, value); }
    public int vacancyStreakDays { get => Read(_file.grid.vacancyStreakDays); set => Write(_file.grid.vacancyStreakDays, value); }
    public int supplyFulfillment { get => Read(_file.grid.supplyFulfillment); set => Write(_file.grid.supplyFulfillment, value); }
    public int customerFulfillment { get => Read(_file.grid.customerFulfillment); set => Write(_file.grid.customerFulfillment, value); }

    int Read(int[]? array) => array != null && _index < array.Length ? array[_index] : 0;

    void Write(int[]? array, int value)
    {
        if (array != null && _index < array.Length)
            array[_index] = value;
    }
}

/// <summary>Nightlife vice row (v51–54).</summary>
public sealed class MetroNightlifeEditor
{
    readonly MetroSaveFile _file;
    readonly int _index;

    public MetroNightlifeEditor(MetroSaveFile file, int index)
    {
        _file = file;
        _index = index;
    }

    public string StorageNote => "Legacy v51–54 — vice lots in nightlife table (merged into workplaces on v55 load)";

    public int lotX { get => Read(_file.nightlife.lotX); set => Write(_file.nightlife.lotX, value); }
    public int lotY { get => Read(_file.nightlife.lotY); set => Write(_file.nightlife.lotY, value); }
    public int viceKind { get => Read(_file.nightlife.viceKind); set => Write(_file.nightlife.viceKind, value); }
    public int ownerGangId { get => Read(_file.nightlife.ownerGangId); set => Write(_file.nightlife.ownerGangId, value); }

    int Read(int[]? array) => array != null && _index < array.Length ? array[_index] : 0;

    void Write(int[]? array, int value)
    {
        if (array != null && _index < array.Length)
            array[_index] = value;
    }
}

public static class WorkplaceKindLabels
{
    public static string SiteKind(int kind) => kind switch
    {
        0 => "Lot",
        1 => "Household",
        _ => kind.ToString()
    };

    public static string Category(int category) => category switch
    {
        0 => "None",
        1 => "Commercial",
        2 => "Office",
        3 => "Industrial",
        4 => "MixedUse",
        5 => "Civic",
        6 => "Utility",
        7 => "Subway",
        8 => "Domestic",
        9 => "HomeOffice",
        _ => category.ToString()
    };

    public static string CategoryFromZone(int zone) => zone switch
    {
        1 => "Residential",
        2 => "Commercial",
        3 => "Industrial",
        4 => "Office",
        5 => "Mixed",
        _ => $"zone {zone}"
    };
}

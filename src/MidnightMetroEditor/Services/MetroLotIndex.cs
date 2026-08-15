using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public static class MetroGridLotHelper
{
    public static bool IsLotAnchor(MetroSaveGrid grid, int x, int y)
    {
        if (grid.width <= 0 || x < 0 || y < 0 || x >= grid.width || y >= grid.height)
            return false;

        var idx = GridIndex(grid, x, y);
        if (grid.lotAnchorX == null || grid.lotAnchorY == null)
            return idx == 0;

        return grid.lotAnchorX[idx] == x && grid.lotAnchorY[idx] == y;
    }

    public static int GridIndex(MetroSaveGrid grid, int x, int y) => y * grid.width + x;
}

public sealed class LotListRow
{
    public int AnchorX { get; init; }
    public int AnchorY { get; init; }
    public int LotWidth { get; init; }
    public int LotHeight { get; init; }
    public int BuiltFloors { get; init; }
    public int Zone { get; init; }
    public int Population { get; init; }
    public string? BuildingItemId { get; init; }
    public object EditTarget { get; init; } = null!;
}

public static class MetroLotIndex
{
    public static List<LotListRow> Build(MetroSaveFile file)
    {
        var rows = new List<LotListRow>();
        var grid = file.grid;
        if (grid.width <= 0 || grid.height <= 0 || grid.type == null)
            return rows;

        for (var y = 0; y < grid.height; y++)
        for (var x = 0; x < grid.width; x++)
        {
            if (!MetroGridLotHelper.IsLotAnchor(grid, x, y))
                continue;

            var idx = MetroGridLotHelper.GridIndex(grid, x, y);
            var type = grid.type[idx];
            if (type == 0 || type == 1)
                continue;

            rows.Add(new LotListRow
            {
                AnchorX = x,
                AnchorY = y,
                LotWidth = ReadInt(grid.lotWidth, idx),
                LotHeight = ReadInt(grid.lotHeight, idx),
                BuiltFloors = ReadInt(grid.builtFloors, idx),
                Zone = ReadInt(grid.zone, idx),
                Population = ReadInt(grid.population, idx),
                BuildingItemId = ReadString(grid.buildingItemId, idx),
                EditTarget = new MetroLotEditor(file, x, y)
            });
        }

        return rows;
    }

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;

    static string? ReadString(string[]? array, int index) =>
        array != null && index >= 0 && index < array.Length ? array[index] : null;

    public static List<EditorTreeItem> BuildTree(MetroSaveFile file, NameDatabase names)
    {
        var roots = new List<EditorTreeItem>();
        foreach (var lot in Build(file))
        {
            var lotNode = new EditorTreeItem
            {
                Label = $"Lot ({lot.AnchorX},{lot.AnchorY}) {lot.LotWidth}x{lot.LotHeight} · {lot.BuiltFloors}f · zone {lot.Zone}",
                EditTarget = lot.EditTarget,
                Children = BuildLotChildren(file, lot.AnchorX, lot.AnchorY, names)
            };
            roots.Add(lotNode);
        }

        return roots;
    }

    static List<EditorTreeItem> BuildLotChildren(MetroSaveFile file, int anchorX, int anchorY, NameDatabase names)
    {
        var children = new List<EditorTreeItem>();

        var workplaces = MetroWorkplaceIndex.GetWorkplacesAtLot(file, anchorX, anchorY);
        var workplaceFolder = new EditorTreeItem
        {
            Label = $"Workplaces ({workplaces.Count})"
        };
        foreach (var wp in workplaces)
        {
            var wpNode = new EditorTreeItem
            {
                Label = wp.Label,
                EditTarget = wp.EditTarget,
                Children = MetroWorkplaceIndex.BuildJobSlotChildren(
                    file,
                    new WorkplaceListRow
                    {
                        WorkplaceId = wp.WorkplaceId,
                        AnchorX = wp.AnchorX,
                        AnchorY = wp.AnchorY,
                        JobSlots = wp.JobSlots
                    },
                    names)
            };
            workplaceFolder.Children.Add(wpNode);
        }
        children.Add(workplaceFolder);

        var households = MetroCitizenSaveQueries.BuildHouseholds(file, anchorX, anchorY, names);
        var householdFolder = new EditorTreeItem
        {
            Label = $"Households ({households.Count})"
        };
        foreach (var household in households)
        {
            var householdNode = new EditorTreeItem
            {
                Label = household.DisplayName,
                EditTarget = new MetroHouseholdEditor(file, household)
            };
            foreach (var memberIndex in household.MemberIndices)
            {
                householdNode.Children.Add(new EditorTreeItem
                {
                    Label = MetroCitizenSaveQueries.FormatResidentName(file.residents, memberIndex, names),
                    EditTarget = new MetroGameResidentEditor(
                        file.residents,
                        memberIndex,
                        CitizenSourceKind.Resident,
                        names,
                        file.grid)
                });
            }

            householdFolder.Children.Add(householdNode);
        }
        children.Add(householdFolder);

        return children;
    }
}

/// <summary>Lot footprint and building state on the grid (not jobs — see workplaces).</summary>
public sealed class MetroLotEditor
{
    readonly MetroSaveFile _file;
    readonly int _index;

    public MetroLotEditor(MetroSaveFile file, int x, int y)
    {
        _file = file;
        AnchorX = x;
        AnchorY = y;
        _index = MetroGridLotHelper.GridIndex(file.grid, x, y);
    }

    public int AnchorX { get; }
    public int AnchorY { get; }

    public int type { get => Read(_file.grid.type); set => Write(_file.grid.type, value); }
    public int zone { get => Read(_file.grid.zone); set => Write(_file.grid.zone, value); }
    public int population { get => Read(_file.grid.population); set => Write(_file.grid.population, value); }
    public int wealth { get => Read(_file.grid.wealth); set => Write(_file.grid.wealth, value); }
    public int ownerRosterId { get => Read(_file.grid.ownerRosterId); set => Write(_file.grid.ownerRosterId, value); }

    public int lotWidth { get => Read(_file.grid.lotWidth); set => Write(_file.grid.lotWidth, value); }
    public int lotHeight { get => Read(_file.grid.lotHeight); set => Write(_file.grid.lotHeight, value); }
    public int lotAnchorX { get => Read(_file.grid.lotAnchorX); set => Write(_file.grid.lotAnchorX, value); }
    public int lotAnchorY { get => Read(_file.grid.lotAnchorY); set => Write(_file.grid.lotAnchorY, value); }
    public int maxFloors { get => Read(_file.grid.maxFloors); set => Write(_file.grid.maxFloors, value); }
    public int builtFloors { get => Read(_file.grid.builtFloors); set => Write(_file.grid.builtFloors, value); }
    public int densityTier { get => Read(_file.grid.densityTier); set => Write(_file.grid.densityTier, value); }

    public string? buildingItemId { get => ReadString(_file.grid.buildingItemId); set => WriteString(_file.grid.buildingItemId, value); }
    public string? rooftopItemId { get => ReadString(_file.grid.rooftopItemId); set => WriteString(_file.grid.rooftopItemId, value); }
    public int embeddedCivicType { get => Read(_file.grid.embeddedCivicType); set => Write(_file.grid.embeddedCivicType, value); }

    public int chunkUnlocked { get => Read(_file.grid.chunkUnlocked); set => Write(_file.grid.chunkUnlocked, value); }

    public int pendingLotWidth { get => Read(_file.grid.pendingLotWidth); set => Write(_file.grid.pendingLotWidth, value); }
    public int pendingLotHeight { get => Read(_file.grid.pendingLotHeight); set => Write(_file.grid.pendingLotHeight, value); }
    public int constructionKind { get => Read(_file.grid.constructionKind); set => Write(_file.grid.constructionKind, value); }
    public int constructionCompleteDay { get => Read(_file.grid.constructionCompleteDay); set => Write(_file.grid.constructionCompleteDay, value); }
    public int constructionStartLotWidth { get => Read(_file.grid.constructionStartLotWidth); set => Write(_file.grid.constructionStartLotWidth, value); }
    public int constructionStartLotHeight { get => Read(_file.grid.constructionStartLotHeight); set => Write(_file.grid.constructionStartLotHeight, value); }
    public int constructionStartBuiltFloors { get => Read(_file.grid.constructionStartBuiltFloors); set => Write(_file.grid.constructionStartBuiltFloors, value); }

    public string Summary =>
        $"Lot ({AnchorX},{AnchorY}) {lotWidth}x{lotHeight} floors={builtFloors}/{maxFloors} zone={zone}";

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

/// <summary>Household group at a residential lot (read-only summary; edit members via children).</summary>
public sealed class MetroHouseholdEditor
{
    public MetroHouseholdEditor(MetroSaveFile file, HouseholdGroupRow group)
    {
        File = file;
        Group = group;
    }

    public MetroSaveFile File { get; }
    public HouseholdGroupRow Group { get; }
    public int headRosterId => Group.HeadRosterId;
    public int anchorX => Group.AnchorX;
    public int anchorY => Group.AnchorY;
    public int memberCount => Group.MemberIndices.Count;
    public string label => Group.DisplayName;
}

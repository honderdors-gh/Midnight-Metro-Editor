using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

/// <summary>Read-only queries over save bulk arrays (mirrors in-game roster/household catalogs).</summary>
public static class MetroCitizenSaveQueries
{
    public const int InvalidAnchor = -1;

    public static (int x, int y) ResolveHomeAnchor(MetroSaveGrid grid, int homeX, int homeY)
    {
        if (homeX < 0 || homeY < 0 || grid.width <= 0 || grid.height <= 0)
            return (homeX, homeY);

        if (homeX >= grid.width || homeY >= grid.height)
            return (homeX, homeY);

        var idx = MetroGridLotHelper.GridIndex(grid, homeX, homeY);
        var ax = grid.lotAnchorX != null && idx < grid.lotAnchorX.Length ? grid.lotAnchorX[idx] : InvalidAnchor;
        var ay = grid.lotAnchorY != null && idx < grid.lotAnchorY.Length ? grid.lotAnchorY[idx] : InvalidAnchor;
        if (ax == InvalidAnchor || ay == InvalidAnchor)
            return (homeX, homeY);

        return ax >= 0 && ay >= 0 && ax < grid.width && ay < grid.height ? (ax, ay) : (homeX, homeY);
    }

    public static List<int> FindResidentIndicesAtHome(MetroSaveFile file, int anchorX, int anchorY)
    {
        var indices = new List<int>();
        var bulk = file.residents;
        if (bulk.rosterId == null)
            return indices;

        var grid = file.grid;
        for (var i = 0; i < bulk.rosterId.Length; i++)
        {
            var hx = ReadInt(bulk.homeX, i, -1);
            var hy = ReadInt(bulk.homeY, i, -1);
            if (hx < 0)
                continue;

            var home = ResolveHomeAnchor(grid, hx, hy);
            if (home.x == anchorX && home.y == anchorY)
                indices.Add(i);
        }

        indices.Sort((a, b) => ReadInt(bulk.rosterId, a).CompareTo(ReadInt(bulk.rosterId, b)));
        return indices;
    }

    public static int FindResidentIndexByRosterId(MetroSaveResidents bulk, int rosterId)
    {
        if (bulk.rosterId == null || rosterId <= 0)
            return -1;

        for (var i = 0; i < bulk.rosterId.Length; i++)
        {
            if (bulk.rosterId[i] == rosterId)
                return i;
        }

        return -1;
    }

    public static List<HouseholdGroupRow> BuildHouseholds(MetroSaveFile file, int anchorX, int anchorY, NameDatabase names)
    {
        var groups = new List<HouseholdGroupRow>();
        var bulk = file.residents;
        var memberIndices = FindResidentIndicesAtHome(file, anchorX, anchorY);
        if (memberIndices.Count == 0)
            return groups;

        var byHead = new Dictionary<int, HouseholdGroupRow>();
        foreach (var memberIndex in memberIndices)
        {
            var headId = ResolveHouseholdHead(bulk, memberIndex);
            if (!byHead.TryGetValue(headId, out var group))
            {
                group = new HouseholdGroupRow
                {
                    HeadRosterId = headId,
                    AnchorX = anchorX,
                    AnchorY = anchorY,
                    DisplayName = FormatResidentName(bulk, memberIndex, names, headId)
                };
                byHead[headId] = group;
                groups.Add(group);
            }

            group.MemberIndices.Add(memberIndex);
        }

        groups.Sort((a, b) => a.HeadRosterId.CompareTo(b.HeadRosterId));
        foreach (var group in groups)
        {
            group.MemberIndices.Sort((a, b) =>
                ReadFloat(bulk.ageYears, a, 0f).CompareTo(ReadFloat(bulk.ageYears, b, 0f)));
            group.DisplayName = FormatHouseholdLabel(bulk, group, names);
        }

        return groups;
    }

    static int ResolveHouseholdHead(MetroSaveResidents bulk, int memberIndex)
    {
        var rosterId = ReadInt(bulk.rosterId, memberIndex);
        var hx = ReadInt(bulk.homeX, memberIndex, -1);
        var hy = ReadInt(bulk.homeY, memberIndex, -1);

        var spouseId = ReadInt(bulk.spouseRosterId, memberIndex);
        if (spouseId > 0)
        {
            var spouseIdx = FindResidentIndexByRosterId(bulk, spouseId);
            if (spouseIdx >= 0
                && ReadInt(bulk.homeX, spouseIdx, -1) == hx
                && ReadInt(bulk.homeY, spouseIdx, -1) == hy)
                return Math.Min(rosterId, spouseId);
        }

        var partnerId = ReadInt(bulk.partnerRosterId, memberIndex);
        if (partnerId > 0)
        {
            var partnerIdx = FindResidentIndexByRosterId(bulk, partnerId);
            if (partnerIdx >= 0
                && ReadInt(bulk.homeX, partnerIdx, -1) == hx
                && ReadInt(bulk.homeY, partnerIdx, -1) == hy)
                return Math.Min(rosterId, partnerId);
        }

        if (ReadFloat(bulk.ageYears, memberIndex, 28f) < 18f)
        {
            var p1 = ReadInt(bulk.parent1RosterId, memberIndex);
            var p1Idx = FindResidentIndexByRosterId(bulk, p1);
            if (p1Idx >= 0
                && ReadInt(bulk.homeX, p1Idx, -1) == hx
                && ReadInt(bulk.homeY, p1Idx, -1) == hy)
                return ResolveHouseholdHead(bulk, p1Idx);

            var p2 = ReadInt(bulk.parent2RosterId, memberIndex);
            var p2Idx = FindResidentIndexByRosterId(bulk, p2);
            if (p2Idx >= 0
                && ReadInt(bulk.homeX, p2Idx, -1) == hx
                && ReadInt(bulk.homeY, p2Idx, -1) == hy)
                return ResolveHouseholdHead(bulk, p2Idx);
        }

        return rosterId;
    }

    public static List<WorkplaceStaffRow> FindStaffAtWorkplace(
        MetroSaveFile file,
        int workplaceId,
        int anchorX,
        int anchorY)
    {
        var staff = new List<WorkplaceStaffRow>();
        var bulk = file.residents;
        if (bulk.rosterId == null)
            return staff;

        for (var i = 0; i < bulk.rosterId.Length; i++)
        {
            var job = ReadInt(bulk.job, i);
            if (job == 0)
                continue;

            var boundId = ReadInt(bulk.workplaceId, i);
            var wx = ReadInt(bulk.workplaceX, i, -1);
            var wy = ReadInt(bulk.workplaceY, i, -1);

            var matches = workplaceId > 0
                ? boundId == workplaceId
                : wx == anchorX && wy == anchorY;

            if (!matches)
                continue;

            staff.Add(new WorkplaceStaffRow
            {
                ResidentIndex = i,
                RosterId = bulk.rosterId[i],
                Job = job,
                SlotIndex = ReadInt(bulk.workSlotIndex, i, -1),
                IsSubwaySlot = ReadInt(bulk.workSubwaySlot, i) != 0
            });
        }

        staff.Sort((a, b) => a.SlotIndex.CompareTo(b.SlotIndex));
        return staff;
    }

    public static string FormatResidentName(MetroSaveResidents bulk, int index, NameDatabase names, int? rosterIdOverride = null)
    {
        var rosterId = rosterIdOverride ?? ReadInt(bulk.rosterId, index);
        return names.ResolveDisplayName(
            rosterId,
            ReadInt(bulk.givenNameId, index),
            ReadInt(bulk.familyNameId, index),
            ReadInt(bulk.appearanceSex, index),
            ReadInt(bulk.appearanceEthnicity, index));
    }

    static string FormatHouseholdLabel(MetroSaveResidents bulk, HouseholdGroupRow group, NameDatabase names)
    {
        var headIdx = -1;
        foreach (var i in group.MemberIndices)
        {
            if (ReadInt(bulk.rosterId, i) != group.HeadRosterId)
                continue;
            headIdx = i;
            break;
        }

        if (headIdx < 0 && group.MemberIndices.Count > 0)
            headIdx = group.MemberIndices[0];

        var headName = FormatResidentName(bulk, headIdx, names, group.HeadRosterId);
        var shortName = headName.Split('(')[0].Trim();
        var count = group.MemberIndices.Count;
        return $"{shortName} · {count} resident{(count == 1 ? "" : "s")} (#{group.HeadRosterId})";
    }

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;

    static float ReadFloat(float[]? array, int index, float defaultValue) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;
}

public sealed class HouseholdGroupRow
{
    public int HeadRosterId { get; set; }
    public int AnchorX { get; init; }
    public int AnchorY { get; init; }
    public string DisplayName { get; set; } = "";
    public List<int> MemberIndices { get; } = new();
}

public sealed class WorkplaceStaffRow
{
    public int ResidentIndex { get; init; }
    public int RosterId { get; init; }
    public int Job { get; init; }
    public int SlotIndex { get; init; }
    public bool IsSubwaySlot { get; init; }
}

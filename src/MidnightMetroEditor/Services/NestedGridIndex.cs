using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public sealed class NestedGridRow
{
    public string Id { get; init; } = "";
    public string? ParentId { get; init; }
    public int Depth { get; init; }
    public bool IsExpandable { get; init; }
    public bool Expanded { get; set; } = true;
    public string Kind { get; init; } = "";
    public string Name { get; init; } = "";
    public string Detail { get; init; } = "";
    public object? EditTarget { get; init; }
}

public static class NestedGridIndex
{
    public static List<NestedGridRow> BuildLotRows(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<NestedGridRow>();
        foreach (var lot in MetroLotIndex.Build(file))
        {
            var lotId = LotId(lot.AnchorX, lot.AnchorY);
            rows.Add(new NestedGridRow
            {
                Id = lotId,
                Depth = 0,
                IsExpandable = true,
                Kind = "Lot",
                Name = $"({lot.AnchorX},{lot.AnchorY})",
                Detail = $"{lot.LotWidth}x{lot.LotHeight} · {lot.BuiltFloors}f · zone {lot.Zone} · pop {lot.Population}",
                EditTarget = lot.EditTarget
            });

            AddLotChildren(rows, file, lot.AnchorX, lot.AnchorY, lotId, names);
        }

        return rows;
    }

    static void AddLotChildren(List<NestedGridRow> rows, MetroSaveFile file, int anchorX, int anchorY, string lotId, NameDatabase names)
    {
        var workplaces = MetroWorkplaceIndex.GetWorkplacesAtLot(file, anchorX, anchorY);
        var wpFolderId = $"{lotId}-workplaces";
        rows.Add(new NestedGridRow
        {
            Id = wpFolderId,
            ParentId = lotId,
            Depth = 1,
            IsExpandable = workplaces.Count > 0,
            Kind = "Group",
            Name = "Workplaces",
            Detail = $"{workplaces.Count} workplace{(workplaces.Count == 1 ? "" : "s")}"
        });

        foreach (var wp in workplaces)
        {
            var wpRowId = $"{wpFolderId}-{wp.WorkplaceId}-{wp.AnchorX}-{wp.AnchorY}";
            rows.Add(new NestedGridRow
            {
                Id = wpRowId,
                ParentId = wpFolderId,
                Depth = 2,
                IsExpandable = wp.JobSlots > 0,
                Kind = "Workplace",
                Name = wp.Label,
                Detail = $"{wp.JobSlots} job slot{(wp.JobSlots == 1 ? "" : "s")}",
                EditTarget = wp.EditTarget
            });

            AddWorkplaceJobSlots(rows, file, wp, wpRowId, names);
        }

        var households = MetroCitizenSaveQueries.BuildHouseholds(file, anchorX, anchorY, names);
        var hhFolderId = $"{lotId}-households";
        rows.Add(new NestedGridRow
        {
            Id = hhFolderId,
            ParentId = lotId,
            Depth = 1,
            IsExpandable = households.Count > 0,
            Kind = "Group",
            Name = "Households",
            Detail = $"{households.Count} household{(households.Count == 1 ? "" : "s")}"
        });

        foreach (var household in households)
        {
            var hhId = $"{hhFolderId}-{household.HeadRosterId}";
            rows.Add(new NestedGridRow
            {
                Id = hhId,
                ParentId = hhFolderId,
                Depth = 2,
                IsExpandable = household.MemberIndices.Count > 0,
                Kind = "Household",
                Name = household.DisplayName,
                Detail = $"{household.MemberIndices.Count} member{(household.MemberIndices.Count == 1 ? "" : "s")}",
                EditTarget = new MetroHouseholdEditor(file, household)
            });

            foreach (var memberIndex in household.MemberIndices)
            {
                rows.Add(new NestedGridRow
                {
                    Id = $"{hhId}-m{memberIndex}",
                    ParentId = hhId,
                    Depth = 3,
                    Kind = "Resident",
                    Name = MetroCitizenSaveQueries.FormatResidentName(file.residents, memberIndex, names),
                    Detail = ResidentOccupationLabels.Format(file.residents, memberIndex, file.grid, CitizenSourceKind.Resident),
                    EditTarget = new MetroGameResidentEditor(file.residents, memberIndex, CitizenSourceKind.Resident, names, file.grid)
                });
            }
        }
    }

    public static List<NestedGridRow> BuildWorkplaceRows(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<NestedGridRow>();
        foreach (var wp in MetroWorkplaceIndex.Build(file))
        {
            var wpId = $"wp-{wp.WorkplaceId}-{wp.AnchorX}-{wp.AnchorY}";
            rows.Add(new NestedGridRow
            {
                Id = wpId,
                Depth = 0,
                IsExpandable = wp.JobSlots > 0,
                Kind = "Workplace",
                Name = wp.WorkplaceId > 0 ? $"#{wp.WorkplaceId} @ ({wp.AnchorX},{wp.AnchorY})" : $"Grid ({wp.AnchorX},{wp.AnchorY})",
                Detail = $"{wp.SiteKind} · {wp.Category} · {wp.JobSlots} slots · W{wp.Wealth}",
                EditTarget = wp.EditTarget
            });

            AddWorkplaceJobSlots(rows, file, new LotWorkplaceRef
            {
                WorkplaceId = wp.WorkplaceId,
                AnchorX = wp.AnchorX,
                AnchorY = wp.AnchorY,
                JobSlots = wp.JobSlots
            }, wpId, names, depth: 1);
        }

        return rows;
    }

    static void AddWorkplaceJobSlots(
        List<NestedGridRow> rows,
        MetroSaveFile file,
        LotWorkplaceRef wp,
        string parentId,
        NameDatabase names,
        int depth = 3)
    {
        var staff = MetroCitizenSaveQueries.FindStaffAtWorkplace(file, wp.WorkplaceId, wp.AnchorX, wp.AnchorY);
        var slotCount = Math.Max(wp.JobSlots, staff.Count > 0 ? staff.Max(s => s.SlotIndex) + 1 : 0);
        if (slotCount <= 0 && staff.Count == 0)
            return;

        if (slotCount <= 0)
            slotCount = staff.Count;

        for (var slot = 0; slot < slotCount; slot++)
        {
            var worker = staff.FirstOrDefault(s => s.SlotIndex == slot && !s.IsSubwaySlot);
            var slotEditor = new MetroWorkplaceJobSlotEditor(
                file,
                names,
                wp.WorkplaceId,
                wp.AnchorX,
                wp.AnchorY,
                slot,
                worker?.ResidentIndex ?? -1);

            rows.Add(new NestedGridRow
            {
                Id = $"{parentId}-slot-{slot}",
                ParentId = parentId,
                Depth = depth,
                Kind = "Job slot",
                Name = slotEditor.SummaryLabel,
                Detail = $"slot {slot}",
                EditTarget = slotEditor
            });
        }

        foreach (var extra in staff.Where(s => s.SlotIndex < 0 || s.SlotIndex >= slotCount))
        {
            var slotEditor = new MetroWorkplaceJobSlotEditor(
                file,
                names,
                wp.WorkplaceId,
                wp.AnchorX,
                wp.AnchorY,
                extra.SlotIndex,
                extra.ResidentIndex);

            rows.Add(new NestedGridRow
            {
                Id = $"{parentId}-slot-x{extra.SlotIndex}",
                ParentId = parentId,
                Depth = depth,
                Kind = "Job slot",
                Name = $"{slotEditor.SummaryLabel} (unassigned)",
                Detail = $"slot {extra.SlotIndex}",
                EditTarget = slotEditor
            });
        }
    }

    static string LotId(int x, int y) => $"lot-{x}-{y}";
}

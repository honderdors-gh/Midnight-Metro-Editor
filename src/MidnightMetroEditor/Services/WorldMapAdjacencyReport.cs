using System.Text;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

/// <summary>
/// Lightweight mirror of game WorldLotAdjacency for Unlimiter-style validation.
/// Hierarchy: Local=0.. Collector=1 Arterial=2 Highway=3 (derived from subtype ranges).
/// </summary>
public static class WorldMapAdjacencyReport
{
    public static string Build(MetroSaveFile file)
    {
        var grid = file.grid;
        if (grid.width <= 0 || grid.height <= 0 || grid.type == null)
            return "No grid.";

        var n = grid.width * grid.height;
        var lotKind = grid.lotKind ?? new int[n];
        var roadSubtype = grid.roadSubtype ?? new int[n];
        var wayGrade = grid.wayGrade ?? new int[n];
        var sb = new StringBuilder();
        var errors = 0;
        const int maxList = 80;

        for (var y = 0; y < grid.height; y++)
        for (var x = 0; x < grid.width; x++)
        {
            var idx = y * grid.width + x;
            var center = Sample(grid, lotKind, roadSubtype, wayGrade, idx, x, y);
            for (var dy = -1; dy <= 1; dy++)
            for (var dx = -1; dx <= 1; dx++)
            {
                if (dx == 0 && dy == 0) continue;
                var nx = x + dx;
                var ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= grid.width || ny >= grid.height) continue;
                var nIdx = ny * grid.width + nx;
                var neighbor = Sample(grid, lotKind, roadSubtype, wayGrade, nIdx, nx, ny);
                if (IsAllowed(center, neighbor, out var reason))
                    continue;

                errors++;
                if (errors <= maxList)
                    sb.AppendLine($"({x},{y})↔({nx},{ny}): {reason}");
            }
        }

        if (errors == 0)
            return $"OK — no adjacency violations on {grid.width}×{grid.height} lots.";

        var header = $"{errors} adjacency issue(s) (showing up to {maxList}):\r\n";
        return header + sb;
    }

    static (int kind, int subtype, int grade, int hier) Sample(
        MetroSaveGrid grid,
        int[] lotKind,
        int[] roadSubtype,
        int[] wayGrade,
        int idx,
        int x,
        int y)
    {
        var kind = idx < lotKind.Length ? lotKind[idx] : InferKind(grid.type![idx]);
        var subtype = idx < roadSubtype.Length ? roadSubtype[idx] : 0;
        var grade = idx < wayGrade.Length ? wayGrade[idx] : 0;
        if (kind == 0 && (grid.type![idx] == 1 || grid.type[idx] == 32))
            kind = 1;
        if (kind == 0 && grid.type![idx] == 31)
            kind = 2;
        return (kind, subtype, grade, Hierarchy(subtype));
    }

    static int InferKind(int cellType) => cellType switch
    {
        31 => 2,
        1 or 32 => 1,
        _ => 0
    };

    static int Hierarchy(int subtype) => subtype switch
    {
        >= 30 => 3, // highway
        >= 20 => 2, // arterial
        >= 10 => 1, // collector
        _ => 0
    };

    static bool IsRamp(int subtype) => subtype is 31 or 32;

    static bool IsAllowed(
        (int kind, int subtype, int grade, int hier) a,
        (int kind, int subtype, int grade, int hier) b,
        out string reason)
    {
        reason = "";
        if (a.kind == 0 && b.kind == 0) return true;

        if (a.kind == 2 || b.kind == 2)
        {
            var road = a.kind == 1 ? a : b.kind == 1 ? b : default;
            if (road.kind != 1) return true;
            if (road.hier == 3 && road.grade == 0)
            {
                reason = "Highway at grade cannot touch river.";
                return false;
            }

            if (road.hier <= 1 && road.grade == 0)
            {
                reason = "Local/collector must meet river via bridge/ramp.";
                return false;
            }

            return true;
        }

        if (a.kind == 1 && b.kind == 0)
            return RoadAllowsZone(a, out reason);
        if (a.kind == 0 && b.kind == 1)
            return RoadAllowsZone(b, out reason);

        if (a.kind == 1 && b.kind == 1)
        {
            if (a.hier == 3 && b.hier < 3 && !IsRamp(a.subtype) && !IsRamp(b.subtype))
            {
                reason = "Highway needs on/off ramp to meet lower road.";
                return false;
            }

            if (b.hier == 3 && a.hier < 3 && !IsRamp(a.subtype) && !IsRamp(b.subtype))
            {
                reason = "Highway needs on/off ramp to meet lower road.";
                return false;
            }

            var step = Math.Abs(a.hier - b.hier);
            if (step > 1 && !IsRamp(a.subtype) && !IsRamp(b.subtype) && a.grade == 0 && b.grade == 0)
            {
                reason = "Hierarchy jump too large without ramp.";
                return false;
            }
        }

        return true;
    }

    static bool RoadAllowsZone((int kind, int subtype, int grade, int hier) road, out string reason)
    {
        reason = "";
        if (road.grade != 0)
        {
            reason = "Non-ground road cannot adjoin zoning.";
            return false;
        }

        if (road.hier >= 2)
        {
            reason = "Arterial/highway cannot adjoin zoning.";
            return false;
        }

        return true;
    }
}

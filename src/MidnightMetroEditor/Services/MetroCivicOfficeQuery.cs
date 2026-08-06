using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

/// <summary>
/// Read-only scan for seated civic roles: mayor, council, judges, DA, prosecutors, police chief.
/// Mirrors <c>MetroSaveElections</c> appointment fields and <see cref="ResidentJob"/> civic jobs.
/// </summary>
public static class MetroCivicOfficeQuery
{
    public static List<CivicOfficeRow> Scan(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<CivicOfficeRow>();
        var seen = new HashSet<(string office, int rosterId)>();
        var elections = file.elections;
        var bulk = file.residents;
        var grid = file.grid;

        void Add(
            string office,
            int rosterId,
            string source,
            string? notes = null,
            bool allowDuplicateOffice = false)
        {
            if (rosterId <= 0)
                return;

            var key = (office, rosterId);
            if (!allowDuplicateOffice && seen.Contains(key))
                return;

            seen.Add(key);
            var index = MetroCitizenSaveQueries.FindResidentIndexByRosterId(bulk, rosterId);
            if (index < 0)
            {
                rows.Add(new CivicOfficeRow
                {
                    Office = office,
                    RosterId = rosterId,
                    Name = "(missing resident)",
                    Occupation = "",
                    Workplace = "",
                    Source = source,
                    Notes = AppendNote(notes, "roster id not in residents table"),
                    EditTarget = null
                });
                return;
            }

            var job = ReadInt(bulk.job, index);
            var policeRank = ReadInt(bulk.policeRank, index);
            rows.Add(new CivicOfficeRow
            {
                Office = office,
                RosterId = rosterId,
                Name = MetroCitizenSaveQueries.FormatResidentName(bulk, index, names),
                Occupation = ResidentOccupationLabels.Format(bulk, index, grid, CitizenSourceKind.Resident),
                Workplace = FormatWorkplace(bulk, index),
                Source = source,
                Notes = AppendNote(notes, ValidateOffice(office, job, policeRank, rosterId, elections)),
                EditTarget = new MetroGameResidentEditor(bulk, index, CitizenSourceKind.Resident, names, grid)
            });
        }

        Add("Mayor", elections.mayorRosterId, "elections.mayorRosterId");
        Add("Police chief", elections.policeChiefRosterId, "elections.policeChiefRosterId");
        Add("District attorney", elections.districtAttorneyRosterId, "elections.districtAttorneyRosterId");

        if (bulk.rosterId != null)
        {
            for (var i = 0; i < bulk.rosterId.Length; i++)
            {
                var rosterId = bulk.rosterId[i];
                var job = ReadInt(bulk.job, i);
                switch ((ResidentJob)job)
                {
                    case ResidentJob.Mayor:
                        Add("Mayor", rosterId, "residents.job=Mayor");
                        break;
                    case ResidentJob.CouncilMember:
                        Add("Council member", rosterId, "residents.job=CouncilMember");
                        break;
                    case ResidentJob.Judge:
                        Add("Judge", rosterId, "residents.job=Judge");
                        break;
                    case ResidentJob.Prosecutor:
                    {
                        var note = rosterId == elections.districtAttorneyRosterId
                            ? "seated DA"
                            : elections.districtAttorneyRosterId <= 0
                                ? "DA slot vacant"
                                : "not seated DA";
                        Add("Prosecutor", rosterId, "residents.job=Prosecutor", note);
                        break;
                    }
                }

                var policeRank = ReadInt(bulk.policeRank, i);
                if (policeRank >= ResidentOccupationLabels.PoliceChiefRank
                    && job is (int)ResidentJob.Professional or (int)ResidentJob.Manager)
                {
                    var note = rosterId == elections.policeChiefRosterId
                        ? "seated chief"
                        : elections.policeChiefRosterId <= 0
                            ? "chief slot vacant"
                            : "command rank, not seated chief";
                    Add("Police commander", rosterId, $"residents.policeRank={policeRank}", note);
                }
            }
        }

        AddCouncilSlateRows(file, names, rows, seen);
        rows.Sort((a, b) => string.Compare(a.SortKey, b.SortKey, StringComparison.OrdinalIgnoreCase));
        return rows;
    }

    static void AddCouncilSlateRows(
        MetroSaveFile file,
        NameDatabase names,
        List<CivicOfficeRow> rows,
        HashSet<(string office, int rosterId)> seen)
    {
        var slate = file.elections.councilSlateMembers;
        var slateSize = Math.Max(1, file.elections.councilSlateSize);
        if (slate == null || slate.Length == 0)
            return;

        var slateCount = slate.Length / slateSize;
        for (var slateIndex = 0; slateIndex < slateCount; slateIndex++)
        {
            for (var member = 0; member < slateSize; member++)
            {
                var idx = slateIndex * slateSize + member;
                if (idx >= slate.Length)
                    break;

                var rosterId = slate[idx];
                if (rosterId <= 0)
                    continue;

                var office = $"Council slate {slateIndex + 1} · seat {member + 1}";
                if (seen.Contains((office, rosterId)))
                    continue;

                seen.Add((office, rosterId));
                var residentIndex = MetroCitizenSaveQueries.FindResidentIndexByRosterId(file.residents, rosterId);
                if (residentIndex < 0)
                {
                    rows.Add(new CivicOfficeRow
                    {
                        Office = office,
                        RosterId = rosterId,
                        Name = "(missing resident)",
                        Source = "elections.councilSlateMembers",
                        Notes = "ballot slate — not seated"
                    });
                    continue;
                }

                var bulk = file.residents;
                rows.Add(new CivicOfficeRow
                {
                    Office = office,
                    RosterId = rosterId,
                    Name = MetroCitizenSaveQueries.FormatResidentName(bulk, residentIndex, names),
                    Occupation = ResidentOccupationLabels.Format(bulk, residentIndex, file.grid, CitizenSourceKind.Resident),
                    Workplace = FormatWorkplace(bulk, residentIndex),
                    Source = "elections.councilSlateMembers",
                    Notes = "ballot slate — not seated",
                    EditTarget = new MetroGameResidentEditor(bulk, residentIndex, CitizenSourceKind.Resident, names, file.grid)
                });
            }
        }
    }

    static string? ValidateOffice(string office, int job, int policeRank, int rosterId, MetroSaveElections elections)
    {
        return office switch
        {
            "Mayor" when job != (int)ResidentJob.Mayor => $"job mismatch ({ResidentJobLabels.Label(job)})",
            "Council member" when job != (int)ResidentJob.CouncilMember => $"job mismatch ({ResidentJobLabels.Label(job)})",
            "Judge" when job != (int)ResidentJob.Judge => $"job mismatch ({ResidentJobLabels.Label(job)})",
            "Prosecutor" when job != (int)ResidentJob.Prosecutor => $"job mismatch ({ResidentJobLabels.Label(job)})",
            "District attorney" when job != (int)ResidentJob.Prosecutor => $"expected Prosecutor, has {ResidentJobLabels.Label(job)}",
            "Police chief" when policeRank < ResidentOccupationLabels.PoliceChiefRank => $"policeRank {policeRank} < chief threshold",
            "Police chief" when job is not ((int)ResidentJob.Professional) and not ((int)ResidentJob.Manager)
                => $"unexpected job {ResidentJobLabels.Label(job)}",
            _ when office == "Mayor" && rosterId != elections.mayorRosterId && elections.mayorRosterId > 0
                => $"another mayor seated in elections ({elections.mayorRosterId})",
            _ => null
        };
    }

    static string FormatWorkplace(MetroSaveResidents bulk, int index)
    {
        var workplaceId = ReadInt(bulk.workplaceId, index);
        var wx = ReadInt(bulk.workplaceX, index, -1);
        var wy = ReadInt(bulk.workplaceY, index, -1);
        if (workplaceId > 0)
            return $"id {workplaceId} @ ({wx},{wy})";
        if (wx >= 0 && wy >= 0)
            return $"({wx},{wy})";
        return "";
    }

    static string? AppendNote(string? existing, string? extra)
    {
        if (string.IsNullOrWhiteSpace(extra))
            return string.IsNullOrWhiteSpace(existing) ? null : existing;
        if (string.IsNullOrWhiteSpace(existing))
            return extra;
        return $"{existing}; {extra}";
    }

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;

    public static string FormatReport(MetroSaveFile file, NameDatabase names)
    {
        var rows = Scan(file, names);
        var elections = file.elections;
        var lines = new List<string>
        {
            "Civic offices",
            $"  Mayor roster id (elections): {FormatId(elections.mayorRosterId)}",
            $"  Police chief roster id: {FormatId(elections.policeChiefRosterId)} (appointed day {FormatDay(elections.policeChiefAppointedDay)})",
            $"  District attorney roster id: {FormatId(elections.districtAttorneyRosterId)} (appointed day {FormatDay(elections.districtAttorneyAppointedDay)})",
            $"  Council slate size: {elections.councilSlateSize}, members: {elections.councilSlateMembers?.Length ?? 0}",
            ""
        };

        if (rows.Count == 0)
        {
            lines.Add("  (no civic officeholders found in residents)");
            return string.Join(Environment.NewLine, lines);
        }

        foreach (var row in rows)
        {
            lines.Add($"  [{row.Office}] #{row.RosterId} {row.Name}");
            if (!string.IsNullOrWhiteSpace(row.Occupation))
                lines.Add($"    occupation: {row.Occupation}");
            if (!string.IsNullOrWhiteSpace(row.Workplace))
                lines.Add($"    workplace: {row.Workplace}");
            lines.Add($"    source: {row.Source}");
            if (!string.IsNullOrWhiteSpace(row.Notes))
                lines.Add($"    note: {row.Notes}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    static string FormatId(int rosterId) => rosterId > 0 ? rosterId.ToString() : "(vacant)";
    static string FormatDay(int day) => day > 0 ? day.ToString() : "—";
}

public sealed class CivicOfficeRow
{
    public string Office { get; init; } = "";
    public int RosterId { get; init; }
    public string Name { get; init; } = "";
    public string Occupation { get; init; } = "";
    public string Workplace { get; init; } = "";
    public string Source { get; init; } = "";
    public string? Notes { get; init; }
    public object? EditTarget { get; init; }

    public string SortKey => $"{OfficeSortRank(Office):D2}|{Office}|{RosterId}";

    static int OfficeSortRank(string office) => office switch
    {
        "Mayor" => 0,
        "Police chief" => 1,
        "District attorney" => 2,
        "Council member" => 3,
        "Judge" => 4,
        "Prosecutor" => 5,
        "Police commander" => 6,
        _ when office.StartsWith("Council slate", StringComparison.Ordinal) => 7,
        _ => 99
    };
}

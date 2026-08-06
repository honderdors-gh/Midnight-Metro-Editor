using System.Text.Json;
using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public static class MetroCrimeIndex
{
    public static List<GangListRow> BuildGangs(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<GangListRow>();
        var gangs = file.gangs;
        if (gangs.gangId == null)
            return rows;

        for (var i = 0; i < gangs.gangId.Length; i++)
        {
            var gangId = gangs.gangId[i];
            var nameId = ReadInt(gangs.nameId, i);
            rows.Add(new GangListRow
            {
                Index = i,
                GangId = gangId,
                Name = GangNameResolver.Resolve(nameId, gangId),
                Boss = FormatRoster(file, names, ReadInt(gangs.bossRosterId, i)),
                BossRosterId = ReadInt(gangs.bossRosterId, i),
                Hq = FormatCoord(ReadInt(gangs.hqX, i, -1), ReadInt(gangs.hqY, i, -1)),
                Notoriety = ReadInt(gangs.notoriety, i),
                MembersRecruited = ReadInt(gangs.membersRecruited, i),
                Activity = ReadInt(gangs.activity, i),
                RivalGangId = ReadInt(gangs.rivalGangId, i),
                RivalryScore = ReadInt(gangs.rivalryScore, i),
                MemberCount = CountGangMembers(file, gangId),
                TurfChunks = CountTurfChunks(file, gangId),
                EditTarget = new MetroGangEditor(file.gangs, i)
            });
        }

        rows.Sort((a, b) => a.GangId.CompareTo(b.GangId));
        return rows;
    }

    public static List<CriminalLedgerRow> BuildLedger(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<CriminalLedgerRow>();
        var ledger = file.criminalLedger;
        if (ledger.rosterId == null)
            return rows;

        for (var i = 0; i < ledger.rosterId.Length; i++)
        {
            var gangId = ReadInt(ledger.gangId, i);
            rows.Add(new CriminalLedgerRow
            {
                Index = i,
                RosterId = ledger.rosterId[i],
                Name = FormatRoster(file, names, ledger.rosterId[i]),
                Day = ReadInt(ledger.day, i),
                CrimeKind = MetroGameLabels.CrimeKind(ReadInt(ledger.crimeKind, i)),
                CrimeKindId = ReadInt(ledger.crimeKind, i),
                Proven = ReadInt(ledger.proven, i) != 0,
                GangId = gangId,
                GangName = gangId > 0 ? GangNameResolver.ResolveByGangId(file, gangId) : "",
                Weight = ReadInt(ledger.weight, i),
                EditTarget = new MetroCriminalLedgerEditor(file.criminalLedger, i)
            });
        }

        rows.Sort((a, b) =>
        {
            var byDay = b.Day.CompareTo(a.Day);
            return byDay != 0 ? byDay : b.Index.CompareTo(a.Index);
        });
        return rows;
    }

    public static List<JusticeCaseRow> BuildCases(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<JusticeCaseRow>();
        var cases = file.justice.cases;
        if (cases == null)
            return rows;

        for (var i = 0; i < cases.Length; i++)
        {
            var c = cases[i];
            rows.Add(new JusticeCaseRow
            {
                Index = i,
                CaseId = c.caseId,
                Offender = FormatRoster(file, names, c.offenderRosterId),
                OffenderRosterId = c.offenderRosterId,
                Victim = FormatRoster(file, names, c.victimRosterId),
                Crime = MetroGameLabels.CrimeKind(c.crimeKind),
                Stage = MetroGameLabels.JusticeStage(c.stage),
                Verdict = MetroGameLabels.JusticeVerdict(c.verdict),
                OpenedDay = c.openedDay,
                TrialDay = c.trialDay,
                Incident = $"({c.incidentX},{c.incidentY})",
                EditTarget = new MetroJusticeCaseEditor(file.justice, i)
            });
        }

        rows.Sort((a, b) => b.OpenedDay.CompareTo(a.OpenedDay));
        return rows;
    }

    public static List<GangMemberRow> BuildGangMembers(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<GangMemberRow>();
        var bulk = file.residents;
        if (bulk.rosterId == null)
            return rows;

        for (var i = 0; i < bulk.rosterId.Length; i++)
        {
            var gangId = ReadInt(bulk.gangId, i);
            if (gangId <= 0)
                continue;

            rows.Add(new GangMemberRow
            {
                Index = i,
                RosterId = bulk.rosterId[i],
                Name = MetroCitizenSaveQueries.FormatResidentName(bulk, i, names),
                GangId = gangId,
                GangName = GangNameResolver.ResolveByGangId(file, gangId),
                Role = MetroGameLabels.GangRole(ReadInt(bulk.gangRole, i)),
                Level = ReadInt(bulk.gangLevel, i),
                Standing = ReadInt(bulk.gangStanding, i),
                Occupation = ResidentOccupationLabels.Format(bulk, i, file.grid, CitizenSourceKind.Resident),
                CriminalRecord = ReadInt(bulk.adultConvictionCount, i) + ReadInt(bulk.juvenileConvictionCount, i),
                EditTarget = new MetroGameResidentEditor(bulk, i, CitizenSourceKind.Resident, names, file.grid)
            });
        }

        rows.Sort((a, b) => a.GangId != b.GangId ? a.GangId.CompareTo(b.GangId) : a.RosterId.CompareTo(b.RosterId));
        return rows;
    }

    static int CountGangMembers(MetroSaveFile file, int gangId)
    {
        var bulk = file.residents;
        if (bulk.rosterId == null || gangId <= 0)
            return 0;

        var count = 0;
        for (var i = 0; i < bulk.rosterId.Length; i++)
        {
            if (ReadInt(bulk.gangId, i) == gangId)
                count++;
        }

        return count;
    }

    static int CountTurfChunks(MetroSaveFile file, int gangId)
    {
        var turf = file.gangTurf.gangId;
        if (turf == null || gangId <= 0)
            return 0;

        var count = 0;
        for (var i = 0; i < turf.Length; i++)
        {
            if (turf[i] == gangId)
                count++;
        }

        return count;
    }

    static string FormatRoster(MetroSaveFile file, NameDatabase names, int rosterId)
    {
        if (rosterId <= 0)
            return "";

        var index = MetroCitizenSaveQueries.FindResidentIndexByRosterId(file.residents, rosterId);
        if (index < 0)
            return $"#{rosterId}";

        return MetroCitizenSaveQueries.FormatResidentName(file.residents, index, names);
    }

    static string FormatCoord(int x, int y) => x >= 0 && y >= 0 ? $"({x},{y})" : "";

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;
}

public static class GangNameResolver
{
    static Dictionary<int, string>? _byId;
    static bool _loaded;

    public static string Resolve(int nameId, int gangId) =>
        nameId > 0 && TryGetName(nameId, out var name) ? name : $"Crew #{gangId}";

    public static string ResolveByGangId(MetroSaveFile file, int gangId)
    {
        var gangs = file.gangs;
        if (gangs.gangId == null)
            return $"Crew #{gangId}";

        for (var i = 0; i < gangs.gangId.Length; i++)
        {
            if (gangs.gangId[i] != gangId)
                continue;
            return Resolve(ReadInt(gangs.nameId, i), gangId);
        }

        return $"Crew #{gangId}";
    }

    static bool TryGetName(int nameId, out string name)
    {
        EnsureLoaded();
        return _byId!.TryGetValue(nameId, out name!);
    }

    static void EnsureLoaded()
    {
        if (_loaded)
            return;

        _loaded = true;
        _byId = new Dictionary<int, string>();

        var gameRoot = GameResourcePaths.FindGameResourcesRoot();
        if (gameRoot == null)
            return;

        var path = Path.Combine(gameRoot, "Names", "Packs", "official_gang_names.json");
        if (!File.Exists(path))
            return;

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (!doc.RootElement.TryGetProperty("gangs", out var gangs) || gangs.ValueKind != JsonValueKind.Array)
                return;

            foreach (var gang in gangs.EnumerateArray())
            {
                if (!gang.TryGetProperty("id", out var idEl) || !gang.TryGetProperty("name", out var nameEl))
                    continue;
                _byId[idEl.GetInt32()] = nameEl.GetString() ?? "";
            }
        }
        catch
        {
            // ignore — fall back to crew ids
        }
    }

    static int ReadInt(int[]? array, int index) =>
        array != null && index >= 0 && index < array.Length ? array[index] : 0;
}

public sealed class GangListRow
{
    public int Index { get; init; }
    public int GangId { get; init; }
    public string Name { get; init; } = "";
    public string Boss { get; init; } = "";
    public int BossRosterId { get; init; }
    public string Hq { get; init; } = "";
    public int Notoriety { get; init; }
    public int MembersRecruited { get; init; }
    public int MemberCount { get; init; }
    public int Activity { get; init; }
    public int RivalGangId { get; init; }
    public int RivalryScore { get; init; }
    public int TurfChunks { get; init; }
    public object EditTarget { get; init; } = null!;
}

public sealed class CriminalLedgerRow
{
    public int Index { get; init; }
    public int RosterId { get; init; }
    public string Name { get; init; } = "";
    public int Day { get; init; }
    public string CrimeKind { get; init; } = "";
    public int CrimeKindId { get; init; }
    public bool Proven { get; init; }
    public int GangId { get; init; }
    public string GangName { get; init; } = "";
    public int Weight { get; init; }
    public object EditTarget { get; init; } = null!;
}

public sealed class JusticeCaseRow
{
    public int Index { get; init; }
    public int CaseId { get; init; }
    public string Offender { get; init; } = "";
    public int OffenderRosterId { get; init; }
    public string Victim { get; init; } = "";
    public string Crime { get; init; } = "";
    public string Stage { get; init; } = "";
    public string Verdict { get; init; } = "";
    public int OpenedDay { get; init; }
    public int TrialDay { get; init; }
    public string Incident { get; init; } = "";
    public object EditTarget { get; init; } = null!;
}

public sealed class GangMemberRow
{
    public int Index { get; init; }
    public int RosterId { get; init; }
    public string Name { get; init; } = "";
    public int GangId { get; init; }
    public string GangName { get; init; } = "";
    public string Role { get; init; } = "";
    public int Level { get; init; }
    public int Standing { get; init; }
    public string Occupation { get; init; } = "";
    public int CriminalRecord { get; init; }
    public object EditTarget { get; init; } = null!;
}

public sealed class MetroGangEditor
{
    readonly MetroSaveGangs _gangs;
    readonly int _index;

    public MetroGangEditor(MetroSaveGangs gangs, int index)
    {
        _gangs = gangs;
        _index = index;
    }

    public int gangId { get => Read(_gangs.gangId); set => Write(_gangs.gangId, value); }
    public int nameId { get => Read(_gangs.nameId); set => Write(_gangs.nameId, value); }
    public int hqX { get => Read(_gangs.hqX); set => Write(_gangs.hqX, value); }
    public int hqY { get => Read(_gangs.hqY); set => Write(_gangs.hqY, value); }
    public int bossRosterId { get => Read(_gangs.bossRosterId); set => Write(_gangs.bossRosterId, value); }
    public int notoriety { get => Read(_gangs.notoriety); set => Write(_gangs.notoriety, value); }
    public int membersRecruited { get => Read(_gangs.membersRecruited); set => Write(_gangs.membersRecruited, value); }
    public int activity { get => Read(_gangs.activity); set => Write(_gangs.activity, value); }
    public int rivalGangId { get => Read(_gangs.rivalGangId); set => Write(_gangs.rivalGangId, value); }
    public int rivalryScore { get => Read(_gangs.rivalryScore); set => Write(_gangs.rivalryScore, value); }

    int Read(int[]? array) => array != null && _index >= 0 && _index < array.Length ? array[_index] : 0;
    void Write(int[]? array, int value)
    {
        if (array != null && _index >= 0 && _index < array.Length)
            array[_index] = value;
    }
}

public sealed class MetroCriminalLedgerEditor
{
    readonly MetroSaveCriminalLedger _ledger;
    readonly int _index;

    public MetroCriminalLedgerEditor(MetroSaveCriminalLedger ledger, int index)
    {
        _ledger = ledger;
        _index = index;
    }

    public int rosterId { get => Read(_ledger.rosterId); set => Write(_ledger.rosterId, value); }
    public int day { get => Read(_ledger.day); set => Write(_ledger.day, value); }
    public int crimeKind { get => Read(_ledger.crimeKind); set => Write(_ledger.crimeKind, value); }
    public int proven { get => Read(_ledger.proven); set => Write(_ledger.proven, value); }
    public int gangId { get => Read(_ledger.gangId); set => Write(_ledger.gangId, value); }
    public int weight { get => Read(_ledger.weight); set => Write(_ledger.weight, value); }

    int Read(int[]? array) => array != null && _index >= 0 && _index < array.Length ? array[_index] : 0;
    void Write(int[]? array, int value)
    {
        if (array != null && _index >= 0 && _index < array.Length)
            array[_index] = value;
    }
}

public sealed class MetroJusticeCaseEditor
{
    readonly MetroSaveJustice _justice;
    readonly int _index;

    public MetroJusticeCaseEditor(MetroSaveJustice justice, int index)
    {
        _justice = justice;
        _index = index;
    }

    MetroSaveJusticeCaseRow Row =>
        _justice.cases != null && _index >= 0 && _index < _justice.cases.Length
            ? _justice.cases[_index]
            : throw new InvalidOperationException("Invalid justice case index");

    public int caseId { get => Row.caseId; set => Row.caseId = value; }
    public int offenderRosterId { get => Row.offenderRosterId; set => Row.offenderRosterId = value; }
    public int victimRosterId { get => Row.victimRosterId; set => Row.victimRosterId = value; }
    public int crimeKind { get => Row.crimeKind; set => Row.crimeKind = value; }
    public int incidentX { get => Row.incidentX; set => Row.incidentX = value; }
    public int incidentY { get => Row.incidentY; set => Row.incidentY = value; }
    public int stage { get => Row.stage; set => Row.stage = value; }
    public int evidenceScore { get => Row.evidenceScore; set => Row.evidenceScore = value; }
    public int openedDay { get => Row.openedDay; set => Row.openedDay = value; }
    public int stageDay { get => Row.stageDay; set => Row.stageDay = value; }
    public int trialDay { get => Row.trialDay; set => Row.trialDay = value; }
    public int incarceratedUntilDay { get => Row.incarceratedUntilDay; set => Row.incarceratedUntilDay = value; }
    public int fineAmount { get => Row.fineAmount; set => Row.fineAmount = value; }
    public int assignedJudgeRosterId { get => Row.assignedJudgeRosterId; set => Row.assignedJudgeRosterId = value; }
    public int verdict { get => Row.verdict; set => Row.verdict = value; }
    public int sentenceDays { get => Row.sentenceDays; set => Row.sentenceDays = value; }
    public int mistrialCount { get => Row.mistrialCount; set => Row.mistrialCount = value; }
    public int pleaCooperating { get => Row.pleaCooperating; set => Row.pleaCooperating = value; }
}

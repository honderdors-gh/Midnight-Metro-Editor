using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public static class MetroElectionsIndex
{
    public static string BuildSummary(MetroSaveFile file, NameDatabase names)
    {
        var e = file.elections;
        var mayor = FormatRoster(file, names, e.mayorRosterId);
        var chief = FormatRoster(file, names, e.policeChiefRosterId);
        var da = FormatRoster(file, names, e.districtAttorneyRosterId);
        var honorCount = e.mayorHonor?.Length ?? 0;
        var mayorCandidates = e.mayorCandidates?.Length ?? 0;
        var councilSlate = e.councilSlateMembers?.Length ?? 0;

        return
            $"Mayor: {(e.mayorRosterId > 0 ? $"#{e.mayorRosterId} {mayor}" : "vacant")}" +
            (e.mayorInterim != 0 ? " (interim)" : "") + "\r\n" +
            $"Police chief: {(e.policeChiefRosterId > 0 ? $"#{e.policeChiefRosterId} {chief}" : "vacant")}" +
            (e.policeChiefAppointedDay > 0 ? $" · appointed day {e.policeChiefAppointedDay}" : "") + "\r\n" +
            $"District attorney: {(e.districtAttorneyRosterId > 0 ? $"#{e.districtAttorneyRosterId} {da}" : "vacant")}" +
            (e.districtAttorneyAppointedDay > 0 ? $" · appointed day {e.districtAttorneyAppointedDay}" : "") + "\r\n" +
            $"Next mayor election: day {e.nextMayorElectionDay} · next council: day {e.nextCouncilElectionDay}\r\n" +
            $"Active race: {MetroGameLabels.ElectionOffice(e.activeRace)}" +
            (e.electionDay > 0 ? $" · election day {e.electionDay}" : "") +
            (e.specialElection != 0 ? " · special" : "") + "\r\n" +
            $"Campaign start: day {e.campaignStartDay} · ballot voters collected: {e.ballotVotersCollected}\r\n" +
            $"Mayor candidates: {mayorCandidates} · council slate entries: {councilSlate} (size {e.councilSlateSize}) · honor wall: {honorCount}";
    }

    public static List<ElectionRosterRow> BuildMayorCandidates(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<ElectionRosterRow>();
        var ids = file.elections.mayorCandidates;
        var votes = file.elections.mayorVotePermille;
        var ballots = file.elections.mayorBallotTotals;
        if (ids == null)
            return rows;

        for (var i = 0; i < ids.Length; i++)
        {
            rows.Add(new ElectionRosterRow
            {
                Slot = i,
                RosterId = ids[i],
                Name = FormatRoster(file, names, ids[i]),
                VotePermille = ReadInt(votes, i),
                BallotTotal = ReadInt(ballots, i),
                Context = "mayor candidate"
            });
        }

        return rows;
    }

    public static List<ElectionRosterRow> BuildCouncilSlate(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<ElectionRosterRow>();
        var ids = file.elections.councilSlateMembers;
        var votes = file.elections.councilSlateVotePermille;
        var ballots = file.elections.councilBallotTotals;
        var slateSize = Math.Max(1, file.elections.councilSlateSize);
        if (ids == null)
            return rows;

        for (var i = 0; i < ids.Length; i++)
        {
            var slateIndex = i / slateSize;
            var seat = i % slateSize;
            rows.Add(new ElectionRosterRow
            {
                Slot = i,
                RosterId = ids[i],
                Name = FormatRoster(file, names, ids[i]),
                VotePermille = ReadInt(votes, i),
                BallotTotal = ReadInt(ballots, i),
                Context = $"slate {slateIndex + 1} seat {seat + 1}"
            });
        }

        return rows;
    }

    public static List<ElectionHonorRow> BuildMayorHonor(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<ElectionHonorRow>();
        var honor = file.elections.mayorHonor;
        if (honor == null)
            return rows;

        for (var i = 0; i < honor.Length; i++)
        {
            var entry = honor[i];
            rows.Add(new ElectionHonorRow
            {
                Index = i,
                RosterId = entry.rosterId,
                Name = FormatRoster(file, names, entry.rosterId),
                TermStartDay = entry.termStartDay,
                TermEndDay = entry.termEndDay,
                ConsecutiveTerms = entry.consecutiveTerms,
                Interim = entry.interim != 0,
                EditTarget = new MetroMayorHonorEditor(file.elections, i)
            });
        }

        return rows;
    }

    static string FormatRoster(MetroSaveFile file, NameDatabase names, int rosterId)
    {
        if (rosterId <= 0)
            return "";

        var index = MetroCitizenSaveQueries.FindResidentIndexByRosterId(file.residents, rosterId);
        if (index < 0)
            index = MetroCitizenSaveQueries.FindResidentIndexByRosterId(file.deceased, rosterId);
        if (index < 0)
            return "(missing)";

        var bulk = index < (file.residents.rosterId?.Length ?? 0) ? file.residents : file.deceased;
        return MetroCitizenSaveQueries.FormatResidentName(bulk, index, names);
    }

    static int ReadInt(int[]? array, int index) =>
        array != null && index >= 0 && index < array.Length ? array[index] : 0;
}

public sealed class ElectionRosterRow
{
    public int Slot { get; init; }
    public int RosterId { get; init; }
    public string Name { get; init; } = "";
    public int VotePermille { get; init; }
    public int BallotTotal { get; init; }
    public string Context { get; init; } = "";
}

public sealed class ElectionHonorRow
{
    public int Index { get; init; }
    public int RosterId { get; init; }
    public string Name { get; init; } = "";
    public int TermStartDay { get; init; }
    public int TermEndDay { get; init; }
    public int ConsecutiveTerms { get; init; }
    public bool Interim { get; init; }
    public object EditTarget { get; init; } = null!;
}

public sealed class MetroMayorHonorEditor
{
    readonly MetroSaveElections _elections;
    readonly int _index;

    public MetroMayorHonorEditor(MetroSaveElections elections, int index)
    {
        _elections = elections;
        _index = index;
    }

    MetroSaveMayorHonorRow Row =>
        _elections.mayorHonor != null && _index >= 0 && _index < _elections.mayorHonor.Length
            ? _elections.mayorHonor[_index]
            : throw new InvalidOperationException("Invalid mayor honor index");

    public int rosterId { get => Row.rosterId; set => Row.rosterId = value; }
    public int termStartDay { get => Row.termStartDay; set => Row.termStartDay = value; }
    public int termEndDay { get => Row.termEndDay; set => Row.termEndDay = value; }
    public int consecutiveTerms { get => Row.consecutiveTerms; set => Row.consecutiveTerms = value; }
    public int interim { get => Row.interim; set => Row.interim = value; }
}

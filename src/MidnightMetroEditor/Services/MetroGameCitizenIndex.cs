using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public static class MetroGameCitizenIndex
{
    public static List<CitizenListRow> Build(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<CitizenListRow>();
        AddBulk(file.residents, CitizenSourceKind.Resident, rows, names, file.grid);
        AddBulk(file.deceased, CitizenSourceKind.Deceased, rows, names, file.grid);
        return rows;
    }

    static void AddBulk(
        MetroSaveResidents bulk,
        CitizenSourceKind source,
        List<CitizenListRow> rows,
        NameDatabase names,
        MetroSaveGrid? grid)
    {
        if (bulk.rosterId == null)
            return;

        for (var i = 0; i < bulk.rosterId.Length; i++)
        {
            var rosterId = bulk.rosterId[i];
            var sex = ReadNullableInt(bulk.appearanceSex, i) ?? 0;
            var eth = ReadNullableInt(bulk.appearanceEthnicity, i) ?? 0;
            var givenId = ReadInt(bulk.givenNameId, i);
            var familyId = ReadInt(bulk.familyNameId, i);
            var workplaceId = ReadInt(bulk.workplaceId, i);
            var gangId = ReadInt(bulk.gangId, i);

            rows.Add(new CitizenListRow
            {
                Source = source,
                Index = i,
                RosterId = rosterId,
                DisplayName = names.ResolveDisplayName(rosterId, givenId, familyId, sex, eth),
                Occupation = ResidentOccupationLabels.Format(bulk, i, grid, source),
                AgeYears = ReadFloat(bulk.ageYears, i, 28f),
                PersonalWealth = 0,
                GangId = gangId,
                IsPregnant = ReadInt(bulk.isPregnant, i) != 0,
                IsKidnapped = false,
                IsRetired = ReadInt(bulk.isRetired, i) != 0,
                Ethnicity = eth,
                Sex = sex,
                EditTarget = new MetroGameResidentEditor(bulk, i, source, names, grid)
            });
        }
    }

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;

    static int? ReadNullableInt(int[]? array, int index) =>
        array != null && index >= 0 && index < array.Length ? array[index] : null;

    static float ReadFloat(float[]? array, int index, float defaultValue) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;
}

public sealed class MetroGameResidentEditor
{
    readonly MetroSaveResidents _bulk;
    readonly int _index;
    readonly CitizenSourceKind _source;
    readonly NameDatabase? _names;
    readonly MetroSaveGrid? _grid;

    public MetroGameResidentEditor(
        MetroSaveResidents bulk,
        int index,
        CitizenSourceKind source,
        NameDatabase? names = null,
        MetroSaveGrid? grid = null)
    {
        _bulk = bulk;
        _index = index;
        _source = source;
        _names = names;
        _grid = grid;
    }

    public int rosterId { get => ReadInt(_bulk.rosterId); set => WriteInt(_bulk.rosterId, value); }
    public string Source => CitizenSourceLabels.Label(_source);

    public string ResolvedGivenName =>
        _names?.ResolveFirstName(givenNameId, appearanceSex, appearanceEthnicity) ?? $"#{givenNameId}";

    public string ResolvedFamilyName =>
        _names?.ResolveFamilyName(familyNameId, appearanceEthnicity) ?? $"#{familyNameId}";

    public int job { get => ReadInt(_bulk.job); set => WriteInt(_bulk.job, value); }
    public string JobLabel => ResidentOccupationLabels.Format(_bulk, _index, _grid, _source);
    public float ageYears { get => ReadFloat(_bulk.ageYears, 28f); set => WriteFloat(_bulk.ageYears, value); }

    public int homeX { get => ReadInt(_bulk.homeX); set => WriteInt(_bulk.homeX, value); }
    public int homeY { get => ReadInt(_bulk.homeY); set => WriteInt(_bulk.homeY, value); }
    public int workplaceX { get => ReadInt(_bulk.workplaceX); set => WriteInt(_bulk.workplaceX, value); }
    public int workplaceY { get => ReadInt(_bulk.workplaceY); set => WriteInt(_bulk.workplaceY, value); }
    public int workplaceId { get => ReadInt(_bulk.workplaceId); set => WriteInt(_bulk.workplaceId, value); }
    public int workSlotIndex { get => ReadInt(_bulk.workSlotIndex); set => WriteInt(_bulk.workSlotIndex, value); }
    public int workSubwaySlot { get => ReadInt(_bulk.workSubwaySlot); set => WriteInt(_bulk.workSubwaySlot, value); }
    public int jobEligibleDay { get => ReadInt(_bulk.jobEligibleDay); set => WriteInt(_bulk.jobEligibleDay, value); }

    public int givenNameId { get => ReadInt(_bulk.givenNameId); set => WriteInt(_bulk.givenNameId, value); }
    public int familyNameId { get => ReadInt(_bulk.familyNameId); set => WriteInt(_bulk.familyNameId, value); }
    public int appearanceSex { get => ReadInt(_bulk.appearanceSex); set => WriteInt(_bulk.appearanceSex, value); }
    public int appearanceEthnicity { get => ReadInt(_bulk.appearanceEthnicity); set => WriteInt(_bulk.appearanceEthnicity, value); }

    public int traitIntelligence { get => ReadInt(_bulk.traitIntelligence); set => WriteInt(_bulk.traitIntelligence, value); }
    public int traitHealth { get => ReadInt(_bulk.traitHealth); set => WriteInt(_bulk.traitHealth, value); }
    public int traitAggression { get => ReadInt(_bulk.traitAggression); set => WriteInt(_bulk.traitAggression, value); }

    public bool isRetired { get => ReadInt(_bulk.isRetired) != 0; set => WriteInt(_bulk.isRetired, value ? 1 : 0); }
    public bool isPregnant { get => ReadInt(_bulk.isPregnant) != 0; set => WriteInt(_bulk.isPregnant, value ? 1 : 0); }

    public int spouseRosterId { get => ReadInt(_bulk.spouseRosterId); set => WriteInt(_bulk.spouseRosterId, value); }
    public int partnerRosterId { get => ReadInt(_bulk.partnerRosterId); set => WriteInt(_bulk.partnerRosterId, value); }
    public int parent1RosterId { get => ReadInt(_bulk.parent1RosterId); set => WriteInt(_bulk.parent1RosterId, value); }
    public int parent2RosterId { get => ReadInt(_bulk.parent2RosterId); set => WriteInt(_bulk.parent2RosterId, value); }

    public int pregnancyDaysRemaining { get => ReadInt(_bulk.pregnancyDaysRemaining); set => WriteInt(_bulk.pregnancyDaysRemaining, value); }
    public int pregnancyFatherRosterId { get => ReadInt(_bulk.pregnancyFatherRosterId); set => WriteInt(_bulk.pregnancyFatherRosterId, value); }

    public int educationLevel { get => ReadInt(_bulk.educationLevel); set => WriteInt(_bulk.educationLevel, value); }
    public float educationYears { get => ReadFloat(_bulk.educationYears, 0f); set => WriteFloat(_bulk.educationYears, value); }

    public int schoolX { get => ReadInt(_bulk.schoolX); set => WriteInt(_bulk.schoolX, value); }
    public int schoolY { get => ReadInt(_bulk.schoolY); set => WriteInt(_bulk.schoolY, value); }
    public int daycareX { get => ReadInt(_bulk.daycareX); set => WriteInt(_bulk.daycareX, value); }
    public int daycareY { get => ReadInt(_bulk.daycareY); set => WriteInt(_bulk.daycareY, value); }
    public int schoolPlacement { get => ReadInt(_bulk.schoolPlacement); set => WriteInt(_bulk.schoolPlacement, value); }
    public int stayHomeParentRosterId { get => ReadInt(_bulk.stayHomeParentRosterId); set => WriteInt(_bulk.stayHomeParentRosterId, value); }

    public int gangId { get => ReadInt(_bulk.gangId); set => WriteInt(_bulk.gangId, value); }
    public int gangRole { get => ReadInt(_bulk.gangRole); set => WriteInt(_bulk.gangRole, value); }
    public int gangLevel { get => ReadInt(_bulk.gangLevel); set => WriteInt(_bulk.gangLevel, value); }
    public int gangStanding { get => ReadInt(_bulk.gangStanding); set => WriteInt(_bulk.gangStanding, value); }

    public int criminalRecord { get => ReadInt(_bulk.criminalRecord); set => WriteInt(_bulk.criminalRecord, value); }
    public bool boloActive { get => ReadInt(_bulk.boloActive) != 0; set => WriteInt(_bulk.boloActive, value ? 1 : 0); }
    public bool warrantActive { get => ReadInt(_bulk.warrantActive) != 0; set => WriteInt(_bulk.warrantActive, value ? 1 : 0); }
    public int boloLotX { get => ReadInt(_bulk.boloLotX); set => WriteInt(_bulk.boloLotX, value); }
    public int boloLotY { get => ReadInt(_bulk.boloLotY); set => WriteInt(_bulk.boloLotY, value); }
    public int warrantLotX { get => ReadInt(_bulk.warrantLotX); set => WriteInt(_bulk.warrantLotX, value); }
    public int warrantLotY { get => ReadInt(_bulk.warrantLotY); set => WriteInt(_bulk.warrantLotY, value); }

    public int incarceratedUntilDay { get => ReadInt(_bulk.incarceratedUntilDay); set => WriteInt(_bulk.incarceratedUntilDay, value); }
    public int residentSinceDay { get => ReadInt(_bulk.residentSinceDay); set => WriteInt(_bulk.residentSinceDay, value); }

    public string? geneticDna { get => ReadString(_bulk.geneticDna); set => WriteString(_bulk.geneticDna, value); }

    int ReadInt(int[]? array, int defaultValue = 0) =>
        array != null && _index >= 0 && _index < array.Length ? array[_index] : defaultValue;

    void WriteInt(int[]? array, int value)
    {
        if (array != null && _index >= 0 && _index < array.Length)
            array[_index] = value;
    }

    float ReadFloat(float[]? array, float defaultValue) =>
        array != null && _index >= 0 && _index < array.Length ? array[_index] : defaultValue;

    void WriteFloat(float[]? array, float value)
    {
        if (array != null && _index >= 0 && _index < array.Length)
            array[_index] = value;
    }

    string? ReadString(string[]? array) =>
        array != null && _index >= 0 && _index < array.Length ? array[_index] : null;

    void WriteString(string[]? array, string? value)
    {
        if (array != null && _index >= 0 && _index < array.Length)
            array[_index] = value;
    }
}

public static class ResidentJobLabels
{
    public static string Label(int job) =>
        Enum.IsDefined(typeof(ResidentJob), job)
            ? ((ResidentJob)job).ToString()
            : job.ToString();
}

public enum ResidentJob
{
    Unemployed = 0,
    Clerk = 1,
    Manager = 2,
    Laborer = 3,
    Professional = 4,
    Nanny = 5,
    Mayor = 6,
    CouncilMember = 7,
    Judge = 8,
    Prosecutor = 9,
    CorrectionsOfficer = 10,
    Bailiff = 11
}

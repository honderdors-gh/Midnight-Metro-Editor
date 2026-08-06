using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

/// <summary>
/// Display occupation strings aligned with <c>Citizen.JobLabel</c> and rank suffixes (police, gang).
/// </summary>
public static class ResidentOccupationLabels
{
    public const int PoliceChiefRank = 7;
    public const int CellTypePoliceAcademy = 26;

    public const float SchoolAgeMinYears = 5f;
    public const float SchoolAgeMaxYears = 18f;

    public static string Format(
        MetroSaveResidents bulk,
        int index,
        MetroSaveGrid? grid,
        CitizenSourceKind source)
    {
        if (source == CitizenSourceKind.Deceased)
            return "Deceased";

        if (ReadInt(bulk.isRetired, index) != 0)
            return "Retired";

        var job = ReadInt(bulk.job, index);
        var age = ReadFloat(bulk.ageYears, index, 28f);

        if (job == (int)ResidentJob.Unemployed)
        {
            if (IsSchoolAge(age))
                return FormatSchoolOccupation(ReadInt(bulk.schoolPlacement, index));
            return "Unemployed";
        }

        if (job == (int)ResidentJob.Clerk && IsPoliceCadet(bulk, index, grid))
            return AppendRankSuffix("Police cadet", bulk, index, grid);

        var label = FormatJobName(job, ReadInt(bulk.policeRank, index));
        return AppendRankSuffix(label, bulk, index, grid);
    }

    static string FormatJobName(int job, int policeRank) => (ResidentJob)job switch
    {
        ResidentJob.Clerk => "Clerk",
        ResidentJob.Manager => "Manager",
        ResidentJob.Laborer => "Laborer",
        ResidentJob.Professional when policeRank >= PoliceChiefRank => "Police chief",
        ResidentJob.Professional when policeRank > 0 => "Police officer",
        ResidentJob.Professional => "Professional",
        ResidentJob.Nanny => "Nanny",
        ResidentJob.Mayor => "Mayor",
        ResidentJob.CouncilMember => "Council member",
        ResidentJob.Judge => "Judge",
        ResidentJob.Prosecutor => "Prosecutor",
        ResidentJob.CorrectionsOfficer => "Corrections officer",
        ResidentJob.Bailiff => "Court bailiff",
        _ => Enum.IsDefined(typeof(ResidentJob), job) ? ((ResidentJob)job).ToString() : $"Job {job}"
    };

    static string AppendRankSuffix(string label, MetroSaveResidents bulk, int index, MetroSaveGrid? grid)
    {
        var job = ReadInt(bulk.job, index);
        var policeRank = ReadInt(bulk.policeRank, index);
        var gangId = ReadInt(bulk.gangId, index);
        var gangRole = ReadInt(bulk.gangRole, index);
        var gangLevel = ReadInt(bulk.gangLevel, index);
        var gangStanding = ReadInt(bulk.gangStanding, index);

        var parts = new List<string>();

        if (job == (int)ResidentJob.Professional && policeRank > 0)
        {
            if (policeRank >= PoliceChiefRank)
                parts.Add($"rank {policeRank}");
            else if (policeRank > 1 || label == "Police officer")
                parts.Add($"rank {policeRank}");
        }

        if (job == (int)ResidentJob.Clerk && IsPoliceCadet(bulk, index, grid))
        {
            var training = ReadInt(bulk.policeTrainingDays, index);
            if (training > 0)
                parts.Add($"training day {training}");
        }

        if (gangId > 0 && gangRole > 0)
        {
            var gangRank = FormatGangRank(gangRole, gangLevel, gangStanding);
            if (!string.IsNullOrEmpty(gangRank))
                parts.Add(gangRank);
        }

        return parts.Count == 0 ? label : $"{label} · {string.Join(" · ", parts)}";
    }

    static string FormatGangRank(int gangRole, int gangLevel, int gangStanding)
    {
        // gangStanding: affiliate vs member — 0 = initiate/affiliate in v50+
        if (gangStanding == 0 && gangRole <= 1)
            return "gang initiate";

        var role = gangRole switch
        {
            4 => "boss",
            3 => "lieutenant",
            2 => "soldier",
            1 => "associate",
            _ => "member"
        };

        return gangLevel > 0 ? $"{role} · level {gangLevel}" : role;
    }

    static string FormatSchoolOccupation(int schoolPlacement) => schoolPlacement switch
    {
        1 => "Student",
        2 => "Student (daycare)",
        3 => "Student (nanny)",
        4 => "Student (home study)",
        _ => "Student"
    };

    static bool IsSchoolAge(float ageYears) =>
        ageYears >= SchoolAgeMinYears && ageYears < SchoolAgeMaxYears;

    static bool IsPoliceCadet(MetroSaveResidents bulk, int index, MetroSaveGrid? grid)
    {
        if (ReadInt(bulk.job, index) != (int)ResidentJob.Clerk)
            return false;

        var wx = ReadInt(bulk.workplaceX, index, -1);
        var wy = ReadInt(bulk.workplaceY, index, -1);
        if (wx < 0 || wy < 0 || grid == null || grid.type == null)
            return ReadInt(bulk.policeTrainingDays, index) > 0;

        var anchor = MetroCitizenSaveQueries.ResolveHomeAnchor(grid, wx, wy);
        if (anchor.x < 0 || anchor.y < 0 || anchor.x >= grid.width || anchor.y >= grid.height)
            return false;

        var idx = MetroGridLotHelper.GridIndex(grid, anchor.x, anchor.y);
        return grid.type[idx] == CellTypePoliceAcademy;
    }

    static int ReadInt(int[]? array, int index, int defaultValue = 0) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;

    static float ReadFloat(float[]? array, int index, float defaultValue) =>
        array != null && index >= 0 && index < array.Length ? array[index] : defaultValue;
}

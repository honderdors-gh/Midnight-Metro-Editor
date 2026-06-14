using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public enum CitizenSourceKind
{
    Resident,
    Agent,
    Deceased,
    Legacy
}

public static class CitizenSourceLabels
{
    public static string Label(CitizenSourceKind kind) => kind switch
    {
        CitizenSourceKind.Resident => "Residents",
        CitizenSourceKind.Agent => "Agents",
        CitizenSourceKind.Deceased => "Deceased",
        CitizenSourceKind.Legacy => "Legacy v1",
        _ => kind.ToString()
    };

    public static string TreeNodeName(CitizenSourceKind? kind) => kind switch
    {
        null => "citizens",
        CitizenSourceKind.Resident => "citizens_residents",
        CitizenSourceKind.Agent => "citizens_agents",
        CitizenSourceKind.Deceased => "citizens_deceased",
        CitizenSourceKind.Legacy => "citizens_legacy",
        _ => "citizens"
    };

    public static CitizenSourceKind? FromTreeNodeName(string view) => view switch
    {
        "citizens" or "citizens_all" => null,
        "citizens_residents" => CitizenSourceKind.Resident,
        "citizens_agents" => CitizenSourceKind.Agent,
        "citizens_deceased" => CitizenSourceKind.Deceased,
        "citizens_legacy" => CitizenSourceKind.Legacy,
        _ => null
    };

    public static int Count(List<CitizenListRow> rows, CitizenSourceKind? kind) =>
        kind == null ? rows.Count : rows.Count(r => r.Source == kind);
}

public sealed class CitizenListRow
{
    public CitizenSourceKind Source { get; init; }
    public int Index { get; init; }
    public int RosterId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Occupation { get; set; } = string.Empty;
    public float AgeYears { get; set; }
    public int PersonalWealth { get; set; }
    public int GangId { get; set; }
    public bool IsPregnant { get; set; }
    public bool IsKidnapped { get; set; }
    public bool IsRetired { get; set; }
    public int Ethnicity { get; set; }
    public int Sex { get; set; }
    public object EditTarget { get; init; } = null!;
}

public static class CitizenIndex
{
    public static List<CitizenListRow> Build(CitySimSaveFile file, NameDatabase names)
    {
        var rows = new List<CitizenListRow>();

        if (file.residents?.rosterId != null)
        {
            var bulk = file.residents;
            for (var i = 0; i < bulk.rosterId.Length; i++)
            {
                var flags = Get(bulk.flags, i);
                rows.Add(new CitizenListRow
                {
                    Source = CitizenSourceKind.Resident,
                    Index = i,
                    RosterId = bulk.rosterId[i],
                    DisplayName = names.ResolveDisplayName(
                        bulk.rosterId[i],
                        Get(bulk.givenNameId, i),
                        Get(bulk.familyNameId, i),
                        GetNullable(bulk.appearanceSex, i),
                        GetNullable(bulk.appearanceEthnicity, i)),
                    Occupation = "Resident",
                    AgeYears = GetFloat(bulk.ageYears, i),
                    PersonalWealth = Get(bulk.personalWealth, i),
                    GangId = Get(bulk.gangAffiliationId, i),
                    IsPregnant = (flags & (int)ResidentFlagBits.Pregnant) != 0,
                    IsKidnapped = (flags & (int)ResidentFlagBits.Kidnapped) != 0,
                    IsRetired = (flags & (int)ResidentFlagBits.Retired) != 0,
                    Ethnicity = Get(bulk.appearanceEthnicity, i),
                    Sex = Get(bulk.appearanceSex, i),
                    EditTarget = new ResidentEditor(bulk, i)
                });
            }
        }

        for (var i = 0; i < file.agents.Count; i++)
        {
            var agent = file.agents[i];
            rows.Add(new CitizenListRow
            {
                Source = CitizenSourceKind.Agent,
                Index = i,
                RosterId = agent.rosterId,
                DisplayName = names.ResolveDisplayName(
                    agent.rosterId,
                    agent.givenNameId,
                    agent.familyNameId,
                    agent.hasAppearance ? agent.characterSex : null,
                    agent.ethnicity,
                    agent.displayName),
                Occupation = OccupationLabels.Label(agent.occupation),
                AgeYears = agent.ageYears,
                PersonalWealth = agent.personalWealth,
                GangId = agent.gangId,
                IsPregnant = agent.isPregnant,
                IsKidnapped = agent.isKidnapped,
                IsRetired = agent.isRetired,
                Ethnicity = agent.ethnicity,
                Sex = agent.characterSex,
                EditTarget = agent
            });
        }

        if (file.deceased?.rosterId != null)
        {
            var bulk = file.deceased;
            for (var i = 0; i < bulk.rosterId.Length; i++)
            {
                rows.Add(new CitizenListRow
                {
                    Source = CitizenSourceKind.Deceased,
                    Index = i,
                    RosterId = bulk.rosterId[i],
                    DisplayName = names.ResolveDisplayName(
                        bulk.rosterId[i],
                        Get(bulk.givenNameId, i),
                        Get(bulk.familyNameId, i),
                        GetNullable(bulk.appearanceSex, i),
                        GetNullable(bulk.appearanceEthnicity, i)) + " [dead]",
                    Occupation = OccupationLabels.Label(Get(bulk.occupation, i)),
                    AgeYears = GetFloat(bulk.ageYears, i),
                    PersonalWealth = 0,
                    GangId = 0,
                    IsPregnant = false,
                    IsKidnapped = false,
                    IsRetired = (Get(bulk.flags, i) & (int)ResidentFlagBits.Retired) != 0,
                    Ethnicity = Get(bulk.appearanceEthnicity, i),
                    Sex = Get(bulk.appearanceSex, i),
                    EditTarget = new DeceasedEditor(bulk, i)
                });
            }
        }

        foreach (var legacy in file.citizens)
        {
            rows.Add(new CitizenListRow
            {
                Source = CitizenSourceKind.Legacy,
                Index = file.citizens.IndexOf(legacy),
                RosterId = legacy.rosterId,
                DisplayName = names.ResolveDisplayName(
                    legacy.rosterId,
                    legacy.givenNameId,
                    legacy.familyNameId,
                    legacy.hasAppearance ? legacy.characterSex : null,
                    legacy.ethnicity,
                    legacy.displayName),
                Occupation = OccupationLabels.Label(legacy.occupation) + " [legacy]",
                AgeYears = legacy.ageYears,
                PersonalWealth = legacy.personalWealth,
                GangId = legacy.gangId,
                IsPregnant = legacy.isPregnant,
                IsKidnapped = legacy.isKidnapped,
                IsRetired = legacy.isRetired,
                Ethnicity = legacy.ethnicity,
                Sex = legacy.characterSex,
                EditTarget = legacy
            });
        }

        return rows.OrderBy(r => r.RosterId).ToList();
    }

    static int Get(int[]? arr, int i) => arr != null && i < arr.Length ? arr[i] : 0;
    static int? GetNullable(int[]? arr, int i) => arr != null && i < arr.Length ? arr[i] : null;
    static float GetFloat(float[]? arr, int i) => arr != null && i < arr.Length ? arr[i] : 0f;
}

/// <summary>Property-grid friendly view of one resident bulk row.</summary>
public sealed class ResidentEditor
{
    readonly SaveResidentBulk _bulk;
    readonly int _i;

    public ResidentEditor(SaveResidentBulk bulk, int index)
    {
        _bulk = bulk;
        _i = index;
    }

    public int rosterId { get => _bulk.rosterId![_i]; set => _bulk.rosterId![_i] = value; }
    public float ageYears { get => _bulk.ageYears![_i]; set => _bulk.ageYears![_i] = value; }
    public float education { get => _bulk.education![_i]; set => _bulk.education![_i] = value; }
    public int personalWealth { get => _bulk.personalWealth![_i]; set => _bulk.personalWealth![_i] = value; }
    public int flags { get => _bulk.flags![_i]; set => _bulk.flags![_i] = value; }
    public int givenNameId { get => _bulk.givenNameId![_i]; set => _bulk.givenNameId![_i] = value; }
    public int familyNameId { get => _bulk.familyNameId![_i]; set => _bulk.familyNameId![_i] = value; }
    public int nicknameId { get => _bulk.nicknameId![_i]; set => _bulk.nicknameId![_i] = value; }
    public int familyId { get => _bulk.familyId![_i]; set => _bulk.familyId![_i] = value; }
    public int spouseRosterId { get => _bulk.spouseRosterId![_i]; set => _bulk.spouseRosterId![_i] = value; }
    public int biologicalMotherRosterId { get => _bulk.biologicalMotherRosterId![_i]; set => _bulk.biologicalMotherRosterId![_i] = value; }
    public int biologicalFatherRosterId { get => _bulk.biologicalFatherRosterId![_i]; set => _bulk.biologicalFatherRosterId![_i] = value; }
    public int parent1RosterId { get => _bulk.parent1RosterId![_i]; set => _bulk.parent1RosterId![_i] = value; }
    public int parent2RosterId { get => _bulk.parent2RosterId![_i]; set => _bulk.parent2RosterId![_i] = value; }
    public int pregnancyDaysRemaining { get => _bulk.pregnancyDaysRemaining![_i]; set => _bulk.pregnancyDaysRemaining![_i] = value; }
    public int pendingFatherRosterId { get => _bulk.pendingFatherRosterId![_i]; set => _bulk.pendingFatherRosterId![_i] = value; }
    public int gangAffiliationId { get => _bulk.gangAffiliationId![_i]; set => _bulk.gangAffiliationId![_i] = value; }
    public int secondaryOccupation { get => _bulk.secondaryOccupation![_i]; set => _bulk.secondaryOccupation![_i] = value; }
    public int secondaryOccupationGangId { get => _bulk.secondaryOccupationGangId![_i]; set => _bulk.secondaryOccupationGangId![_i] = value; }
    public int sceneInjuryLevel { get => _bulk.sceneInjuryLevel![_i]; set => _bulk.sceneInjuryLevel![_i] = value; }
    public int kidnapCaptorRosterId { get => _bulk.kidnapCaptorRosterId![_i]; set => _bulk.kidnapCaptorRosterId![_i] = value; }
    public int lastKidnapPerpetratorRosterId { get => _bulk.lastKidnapPerpetratorRosterId![_i]; set => _bulk.lastKidnapPerpetratorRosterId![_i] = value; }
    public float injuryPenaltyYears { get => _bulk.injuryPenaltyYears![_i]; set => _bulk.injuryPenaltyYears![_i] = value; }
    public float lifespanModifierYears { get => _bulk.lifespanModifierYears![_i]; set => _bulk.lifespanModifierYears![_i] = value; }
    public float householdIncomeMultiplier { get => _bulk.householdIncomeMultiplier![_i]; set => _bulk.householdIncomeMultiplier![_i] = value; }
    public int packedHome { get => _bulk.packedHome![_i]; set => _bulk.packedHome![_i] = value; }
    public int packedWorkplace { get => _bulk.packedWorkplace![_i]; set => _bulk.packedWorkplace![_i] = value; }
    public int packedSceneInjuryBuilding { get => _bulk.packedSceneInjuryBuilding![_i]; set => _bulk.packedSceneInjuryBuilding![_i] = value; }
    public int packedKidnapHideout { get => _bulk.packedKidnapHideout![_i]; set => _bulk.packedKidnapHideout![_i] = value; }
    public string? geneticDna { get => _bulk.geneticDna![_i]; set => _bulk.geneticDna![_i] = value; }
    public int appearanceSex { get => _bulk.appearanceSex![_i]; set => _bulk.appearanceSex![_i] = value; }
    public int appearanceEthnicity { get => _bulk.appearanceEthnicity![_i]; set => _bulk.appearanceEthnicity![_i] = value; }
    public int appearanceBodyType { get => _bulk.appearanceBodyType![_i]; set => _bulk.appearanceBodyType![_i] = value; }
    public float appearanceHeightMeters { get => _bulk.appearanceHeightMeters![_i]; set => _bulk.appearanceHeightMeters![_i] = value; }
    public int gangBrothelConsecutiveCount { get => _bulk.gangBrothelConsecutiveCount![_i]; set => _bulk.gangBrothelConsecutiveCount![_i] = value; }
    public int gangBrothelLastWorkDay { get => _bulk.gangBrothelLastWorkDay![_i]; set => _bulk.gangBrothelLastWorkDay![_i] = value; }
    public string? packedGangBrothelKidsAtRank { get => _bulk.packedGangBrothelKidsAtRank![_i]; set => _bulk.packedGangBrothelKidsAtRank![_i] = value; }

    public bool employed { get => GetFlag(ResidentFlagBits.Employed); set => SetFlag(ResidentFlagBits.Employed, value); }
    public bool isPregnant { get => GetFlag(ResidentFlagBits.Pregnant); set => SetFlag(ResidentFlagBits.Pregnant, value); }
    public bool isRetired { get => GetFlag(ResidentFlagBits.Retired); set => SetFlag(ResidentFlagBits.Retired, value); }
    public bool isKidnapped { get => GetFlag(ResidentFlagBits.Kidnapped); set => SetFlag(ResidentFlagBits.Kidnapped, value); }
    public bool isGangBrokenIn { get => GetFlag(ResidentFlagBits.GangBrokenIn); set => SetFlag(ResidentFlagBits.GangBrokenIn, value); }
    public bool isGangBrothelFreeUse { get => GetFlag(ResidentFlagBits.GangBrothelFreeUse); set => SetFlag(ResidentFlagBits.GangBrothelFreeUse, value); }
    public bool neighborhoodPredator { get => GetFlag(ResidentFlagBits.NeighborhoodPredator); set => SetFlag(ResidentFlagBits.NeighborhoodPredator, value); }

    bool GetFlag(ResidentFlagBits bit) => (_bulk.flags![_i] & (int)bit) != 0;

    void SetFlag(ResidentFlagBits bit, bool value)
    {
        if (value)
            _bulk.flags![_i] |= (int)bit;
        else
            _bulk.flags![_i] &= ~(int)bit;
    }
}

public sealed class DeceasedEditor
{
    readonly SaveDeceasedBulk _bulk;
    readonly int _i;

    public DeceasedEditor(SaveDeceasedBulk bulk, int index)
    {
        _bulk = bulk;
        _i = index;
    }

    public int rosterId { get => _bulk.rosterId![_i]; set => _bulk.rosterId![_i] = value; }
    public int occupation { get => _bulk.occupation![_i]; set => _bulk.occupation![_i] = value; }
    public float ageYears { get => _bulk.ageYears![_i]; set => _bulk.ageYears![_i] = value; }
    public float education { get => _bulk.education![_i]; set => _bulk.education![_i] = value; }
    public int givenNameId { get => _bulk.givenNameId![_i]; set => _bulk.givenNameId![_i] = value; }
    public int familyNameId { get => _bulk.familyNameId![_i]; set => _bulk.familyNameId![_i] = value; }
    public int familyId { get => _bulk.familyId![_i]; set => _bulk.familyId![_i] = value; }
    public int spouseRosterId { get => _bulk.spouseRosterId![_i]; set => _bulk.spouseRosterId![_i] = value; }
    public int biologicalMotherRosterId { get => _bulk.biologicalMotherRosterId![_i]; set => _bulk.biologicalMotherRosterId![_i] = value; }
    public int biologicalFatherRosterId { get => _bulk.biologicalFatherRosterId![_i]; set => _bulk.biologicalFatherRosterId![_i] = value; }
    public string? geneticDna { get => _bulk.geneticDna![_i]; set => _bulk.geneticDna![_i] = value; }
    public int appearanceSex { get => _bulk.appearanceSex![_i]; set => _bulk.appearanceSex![_i] = value; }
    public int appearanceEthnicity { get => _bulk.appearanceEthnicity![_i]; set => _bulk.appearanceEthnicity![_i] = value; }
}

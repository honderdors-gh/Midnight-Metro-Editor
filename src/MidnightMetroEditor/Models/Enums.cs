namespace MidnightMetroEditor.Models;

public enum CellType
{
    Empty,
    Street,
    Store,
    Office,
    Apartment,
    RowHouse,
    PoliceStation,
    FireStation,
    MedicalStation,
    PoliceStationMedium,
    FireStationMedium,
    MedicalStationMedium,
    CrimeBlock,
    Courthouse,
    CityHall,
    Jail,
    JailCell,
    PrimarySchool,
    SecondarySchool,
    HighSchool,
    College,
    University,
    Industrial
}

public enum ZoneType
{
    Residential = 0,
    Commercial = 1,
    Industrial = 2,
    MixedResidentialCommercial = 3,
    Office = 4,
    Civic = 5,
    Gang = 6,
    None = 7,
    Mixed = MixedResidentialCommercial
}

public enum CitizenOccupation
{
    None,
    Resident,
    Police,
    Fire,
    Medical,
    Criminal,
    Judge,
    CourtPolice,
    JailGuard
}

public enum ResidentFlagBits
{
    Employed = 1 << 0,
    DroppedOut = 1 << 1,
    Pregnant = 1 << 2,
    Retired = 1 << 3,
    HasAppearance = 1 << 4,
    Kidnapped = 1 << 5,
    NeighborhoodPredator = 1 << 6,
    GangBrokenIn = 1 << 7,
    GangBrothelFreeUse = 1 << 28,
    PaternityDnaLocked = 1 << 29
}

public static class OccupationLabels
{
    public static string Label(int occupation) =>
        Enum.IsDefined(typeof(CitizenOccupation), occupation)
            ? ((CitizenOccupation)occupation).ToString()
            : occupation.ToString();

    public static string CellTypeLabel(int type) =>
        Enum.IsDefined(typeof(CellType), type)
            ? ((CellType)type).ToString()
            : type.ToString();

    public static string ZoneLabel(int zone) =>
        Enum.IsDefined(typeof(ZoneType), zone)
            ? ((ZoneType)zone).ToString()
            : zone.ToString();
}

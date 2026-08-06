namespace MidnightMetroEditor.Services;

/// <summary>Display labels aligned with Midnight Metro simulation enums.</summary>
public static class MetroGameLabels
{
    public static string NewsCategory(int category) => category switch
    {
        0 => "Crime",
        1 => "Police",
        2 => "Policy",
        3 => "Transport",
        4 => "City",
        5 => "Economy",
        _ => $"Category {category}"
    };

    public static string ElectionOffice(int office) => office switch
    {
        0 => "None",
        1 => "Mayor",
        2 => "Council",
        _ => $"Office {office}"
    };

    public static string GangRole(int role) => role switch
    {
        0 => "None",
        1 => "Associate",
        2 => "Soldier",
        3 => "Lieutenant",
        4 => "Boss",
        _ => $"Role {role}"
    };

    public static string JusticeStage(int stage) => stage switch
    {
        0 => "None",
        2 => "Detained",
        3 => "Investigating",
        4 => "Booked",
        5 => "Pre-trial",
        6 => "Trial",
        7 => "Verdict",
        8 => "Sentencing",
        9 => "Incarcerated",
        10 => "Closed",
        _ => $"Stage {stage}"
    };

    public static string JusticeVerdict(int verdict) => verdict switch
    {
        0 => "None",
        1 => "Guilty",
        2 => "Not guilty",
        3 => "Mistrial",
        4 => "Dismissed",
        5 => "Diversion",
        _ => $"Verdict {verdict}"
    };

    public static string CrimeKind(int kind) => kind switch
    {
        0 => "None",
        1 => "Pickpocket",
        2 => "Vandalism",
        3 => "Simple assault",
        4 => "Commercial theft",
        5 => "Vehicle theft",
        6 => "Bike theft",
        _ => $"Crime {kind}"
    };
}

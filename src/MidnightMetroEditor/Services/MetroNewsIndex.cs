using MidnightMetroEditor.Models;

namespace MidnightMetroEditor.Services;

public static class MetroNewsIndex
{
    public static List<NewsListRow> Build(MetroSaveFile file, NameDatabase names)
    {
        var rows = new List<NewsListRow>();
        var articles = file.news.articles;
        if (articles == null)
            return rows;

        for (var i = 0; i < articles.Length; i++)
        {
            var article = articles[i];
            rows.Add(new NewsListRow
            {
                Index = i,
                Id = article.id,
                Day = article.day,
                Hour = article.hour,
                Category = MetroGameLabels.NewsCategory(article.category),
                Outlet = article.outlet ?? "",
                Headline = article.headline ?? "",
                Subhead = Truncate(article.subhead, 80),
                Subject = FormatRoster(file, names, article.subjectRosterId),
                Victim = FormatRoster(file, names, article.victimRosterId),
                Lot = article.lotX >= 0 ? $"({article.lotX},{article.lotY})" : "",
                Read = article.readFlag != 0,
                Bolo = article.boloFlag != 0,
                EditTarget = new MetroNewsArticleEditor(file.news, i)
            });
        }

        rows.Sort((a, b) =>
        {
            var byDay = b.Day.CompareTo(a.Day);
            return byDay != 0 ? byDay : b.Hour.CompareTo(a.Hour);
        });
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
            return $"#{rosterId}";

        var bulk = index < (file.residents.rosterId?.Length ?? 0)
            ? file.residents
            : file.deceased;
        return MetroCitizenSaveQueries.FormatResidentName(bulk, index, names);
    }

    static string Truncate(string? text, int max) =>
        string.IsNullOrEmpty(text) ? "" : text.Length <= max ? text : text[..max] + "…";
}

public sealed class NewsListRow
{
    public int Index { get; init; }
    public int Id { get; init; }
    public int Day { get; init; }
    public float Hour { get; init; }
    public string Category { get; init; } = "";
    public string Outlet { get; init; } = "";
    public string Headline { get; init; } = "";
    public string Subhead { get; init; } = "";
    public string Subject { get; init; } = "";
    public string Victim { get; init; } = "";
    public string Lot { get; init; } = "";
    public bool Read { get; init; }
    public bool Bolo { get; init; }
    public object EditTarget { get; init; } = null!;
}

public sealed class MetroNewsArticleEditor
{
    readonly MetroSaveNews _news;
    readonly int _index;

    public MetroNewsArticleEditor(MetroSaveNews news, int index)
    {
        _news = news;
        _index = index;
    }

    MetroSaveNewsArticleRow Row =>
        _news.articles != null && _index >= 0 && _index < _news.articles.Length
            ? _news.articles[_index]
            : throw new InvalidOperationException("Invalid news article index");

    public int id { get => Row.id; set => Row.id = value; }
    public int day { get => Row.day; set => Row.day = value; }
    public float hour { get => Row.hour; set => Row.hour = value; }
    public int category { get => Row.category; set => Row.category = value; }
    public string outlet { get => Row.outlet; set => Row.outlet = value; }
    public string headline { get => Row.headline; set => Row.headline = value; }
    public string subhead { get => Row.subhead; set => Row.subhead = value; }
    public string body { get => Row.body; set => Row.body = value; }
    public int lotX { get => Row.lotX; set => Row.lotX = value; }
    public int lotY { get => Row.lotY; set => Row.lotY = value; }
    public int subjectRosterId { get => Row.subjectRosterId; set => Row.subjectRosterId = value; }
    public int victimRosterId { get => Row.victimRosterId; set => Row.victimRosterId = value; }
    public int crimeKind { get => Row.crimeKind; set => Row.crimeKind = value; }
    public float crimePressurePercent { get => Row.crimePressurePercent; set => Row.crimePressurePercent = value; }
    public int readFlag { get => Row.readFlag; set => Row.readFlag = value; }
    public int boloFlag { get => Row.boloFlag; set => Row.boloFlag = value; }
    public int metroLineId { get => Row.metroLineId; set => Row.metroLineId = value; }
    public int metroStationsCount { get => Row.metroStationsCount; set => Row.metroStationsCount = value; }
    public int metroTunnelCells { get => Row.metroTunnelCells; set => Row.metroTunnelCells = value; }
    public int metroLinesOpened { get => Row.metroLinesOpened; set => Row.metroLinesOpened = value; }
}

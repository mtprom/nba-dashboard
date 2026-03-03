namespace NbaDashboard.Infrastructure.NbaStats;

public static class NbaStatsHeaders
{
    public static readonly Dictionary<string, string> Default = new()
    {
        ["Accept"]          = "application/json, text/plain, */*",
        ["Accept-Language"] = "en-US,en;q=0.9",
        ["Connection"]      = "keep-alive",
        ["Host"]            = "stats.nba.com",
        ["Origin"]          = "https://www.nba.com",
        ["Referer"]         = "https://www.nba.com/",
        ["User-Agent"]      = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                            + "AppleWebKit/537.36 (KHTML, like Gecko) "
                            + "Chrome/120.0.0.0 Safari/537.36",
        ["x-nba-stats-origin"] = "stats",
        ["x-nba-stats-token"]  = "true",
    };
}

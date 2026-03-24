namespace NbaDashboard.Infrastructure.NbaStats.Models;

// leaguestandingsv3 returns the classic v2 envelope:
// { "resultSets": [{ "name": "Standings", "headers": [...], "rowSet": [[...], ...] }] }
// Key headers: TeamID, TeamCity, TeamName, TeamAbbreviation, Conference, Division,
// WINS, LOSSES, WinPCT, PlayoffRank, HOME, ROAD, L10, strCurrentStreak,
// OffRating, DefRating, NetRating, Pace

public class LeagueStandingsV3Response
{
    public List<StandingsResultSet> ResultSets { get; set; } = [];
}

public class StandingsResultSet
{
    public string Name { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = [];
    public List<List<System.Text.Json.JsonElement>> RowSet { get; set; } = [];
}

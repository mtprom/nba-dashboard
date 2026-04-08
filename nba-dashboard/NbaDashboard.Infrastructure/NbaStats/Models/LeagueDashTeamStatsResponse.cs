namespace NbaDashboard.Infrastructure.NbaStats.Models;

// leaguedashteamstats returns the classic v2 envelope:
// { "resultSets": [{ "name": "...", "headers": [...], "rowSet": [[...], ...] }] }
//
// For MeasureType=Advanced, PerMode=PerGame the live payload includes:
//   TEAM_ID, TEAM_NAME, E_OFF_RATING, OFF_RATING, E_DEF_RATING, DEF_RATING,
//   E_NET_RATING, NET_RATING, E_PACE, PACE, ...

public class LeagueDashTeamStatsResponse
{
    public List<TeamStatsResultSet> ResultSets { get; set; } = [];
}

public class TeamStatsResultSet
{
    public string Name { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = [];
    public List<List<System.Text.Json.JsonElement>> RowSet { get; set; } = [];
}

namespace NbaDashboard.Infrastructure.NbaStats.Models;

// leaguedashplayerstats returns the classic v2 envelope:
// { "resultSets": [{ "name": "...", "headers": [...], "rowSet": [[...], ...] }] }
//
// Traditional (MeasureType=Base, PerMode=PerGame) headers:
//   PLAYER_ID, PLAYER_NAME, NICKNAME, TEAM_ID, TEAM_ABBREVIATION, AGE,
//   GP, W, L, W_PCT, MIN, FGM, FGA, FG_PCT, FG3M, FG3A, FG3_PCT,
//   FTM, FTA, FT_PCT, OREB, DREB, REB, AST, TOV, STL, BLK, ...PTS...
//
// Advanced (MeasureType=Advanced, PerMode=PerGame) headers:
//   PLAYER_ID, PLAYER_NAME, NICKNAME, TEAM_ID, TEAM_ABBREVIATION, AGE,
//   GP, ...NET_RATING, ...TS_PCT, USG_PCT, ...PIE...

public class LeagueDashPlayerStatsResponse
{
    public List<PlayerStatsResultSet> ResultSets { get; set; } = [];
}

public class PlayerStatsResultSet
{
    public string Name { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = [];
    public List<List<System.Text.Json.JsonElement>> RowSet { get; set; } = [];
}

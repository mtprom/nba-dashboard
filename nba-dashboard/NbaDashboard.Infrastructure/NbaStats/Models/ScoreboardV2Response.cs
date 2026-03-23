namespace NbaDashboard.Infrastructure.NbaStats.Models;

// scoreboardv2 returns the classic v2 envelope with multiple resultSets:
// "GameHeader" — one row per game: GAME_ID, GAME_STATUS_TEXT, HOME_TEAM_ID, VISITOR_TEAM_ID, ARENA_NAME, etc.
// "LineScore" — one row per team per game: GAME_ID, TEAM_ID, PTS, etc.

public class ScoreboardV2Response
{
    public List<ScoreboardResultSet> ResultSets { get; set; } = [];
}

public class ScoreboardResultSet
{
    public string Name { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = [];
    public List<List<System.Text.Json.JsonElement>> RowSet { get; set; } = [];
}

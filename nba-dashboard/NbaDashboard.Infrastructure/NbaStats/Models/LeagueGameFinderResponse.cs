using System.Text.Json.Serialization;

namespace NbaDashboard.Infrastructure.NbaStats.Models;

// leaguegamefinder returns the classic v2 envelope:
// { "resultSets": [{ "name": "...", "headers": [...], "rowSet": [[...], ...] }] }
// Each row = one team's view of a game. A game appears twice (once per team).
// Column order matches headers array: SEASON_ID, TEAM_ID, TEAM_ABBREVIATION,
// TEAM_NAME, GAME_ID, GAME_DATE, MATCHUP, WL, MIN, PTS, ...

public class LeagueGameFinderResponse
{
    public List<GameFinderResultSet> ResultSets { get; set; } = [];
}

public class GameFinderResultSet
{
    public string Name { get; set; } = string.Empty;
    public List<string> Headers { get; set; } = [];

    // Each inner list is a game-team row; values match Headers by position.
    // Using JsonElement so we can safely coerce ints, strings, and floats.
    public List<List<System.Text.Json.JsonElement>> RowSet { get; set; } = [];
}

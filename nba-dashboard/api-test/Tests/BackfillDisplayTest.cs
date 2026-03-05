using System.Text.Json;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace ApiTest.Tests;

/// <summary>
/// Fetches 5 days of game data (Oct 22-26 2024, opening week of 2024-25 season)
/// and prints all games with top scorers per team.
/// ~36 API calls, takes ~3 minutes to run.
/// </summary>
public static class BackfillDisplayTest
{
    private const string DateFrom = "2024-10-22";
    private const string DateTo   = "2024-10-26";

    public static async Task RunAsync()
    {
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        var handler = new HttpClientHandler();
        NbaStatsHeaders.ConfigureHandler(handler);

        using var http = new HttpClient(handler);
        foreach (var (key, value) in NbaStatsHeaders.Default)
            http.DefaultRequestHeaders.TryAddWithoutValidation(key, value);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        Console.WriteLine($"=== Backfill display: {DateFrom} to {DateTo} ===\n");

        // Step 1: get all games in the date range (single call)
        var finderJson = await http.GetStringAsync(
            $"https://stats.nba.com/stats/leaguegamefinder" +
            $"?LeagueID=00&Season=2024-25&SeasonType=Regular%20Season" +
            $"&DateFrom={DateFrom}&DateTo={DateTo}");

        await Task.Delay(5000);

        var finder = JsonSerializer.Deserialize<LeagueGameFinderResponse>(finderJson, jsonOptions);
        var rs = finder?.ResultSets?.FirstOrDefault();
        if (rs == null || rs.RowSet.Count == 0) { Console.WriteLine("No games found."); return; }

        var headers   = rs.Headers;
        int gameIdIdx = headers.IndexOf("GAME_ID");
        int dateIdx   = headers.IndexOf("GAME_DATE");

        // Deduplicate games, preserving date order
        var games = rs.RowSet
            .Select(row => new
            {
                GameId = row[gameIdIdx].GetString()!,
                Date   = row[dateIdx].GetString()!,
            })
            .DistinctBy(g => g.GameId)
            .OrderBy(g => g.Date)
            .ToList();

        Console.WriteLine($"Found {games.Count} games.\n");

        // Step 2: fetch box score for each game and display
        foreach (var game in games)
        {
            Console.WriteLine($"--- {game.Date}  Game {game.GameId} ---");

            var tradJson = await http.GetStringAsync(
                $"https://stats.nba.com/stats/boxscoretraditionalv3?GameID={game.GameId}");

            await Task.Delay(5000);

            var trad = JsonSerializer.Deserialize<BoxScoreTraditionalV3Response>(tradJson, jsonOptions);
            var bs = trad?.BoxScoreTraditional;
            if (bs == null) { Console.WriteLine("  (no data)"); continue; }

            var homeScore = bs.HomeTeam.Players.Sum(p => p.Statistics.Points);
            var awayScore = bs.AwayTeam.Players.Sum(p => p.Statistics.Points);

            Console.WriteLine($"  {bs.HomeTeam.TeamTricode} {homeScore}  vs  {bs.AwayTeam.TeamTricode} {awayScore}");
            PrintTopScorers("  Home", bs.HomeTeam.Players);
            PrintTopScorers("  Away", bs.AwayTeam.Players);
            Console.WriteLine();
        }

        Console.WriteLine("=== Done ===");
    }

    private static void PrintTopScorers(string label, List<TraditionalPlayer> players)
    {
        var top = players
            .Where(p => p.Statistics.Minutes != "")
            .OrderByDescending(p => p.Statistics.Points)
            .Take(3);

        foreach (var p in top)
        {
            var s = p.Statistics;
            Console.WriteLine(
                $"  {label}  {p.FirstName[0]}. {p.FamilyName,-18} " +
                $"{s.Points,2}pts  {s.ReboundsTotal}reb  {s.Assists}ast  " +
                $"{s.FieldGoalsMade}/{s.FieldGoalsAttempted}fg  " +
                $"{s.MinutesDecimal():F0}min");
        }
    }
}

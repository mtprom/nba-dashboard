using System.Text.Json;
using NbaDashboard.Infrastructure.NbaStats;
using NbaDashboard.Infrastructure.NbaStats.Models;

namespace ApiTest.Tests;

/// <summary>
/// Validates that all three response models deserialize correctly from the real API.
/// Prints key fields to confirm mapping is accurate.
/// </summary>
public static class ModelValidationTest
{
    public static async Task RunAsync()
    {
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        var handler = new HttpClientHandler();
        NbaStatsHeaders.ConfigureHandler(handler);

        using var http = new HttpClient(handler);
        foreach (var (key, value) in NbaStatsHeaders.Default)
            http.DefaultRequestHeaders.TryAddWithoutValidation(key, value);

        var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        // --- Test 1: leaguegamefinder → LeagueGameFinderResponse ---
        Console.WriteLine("=== leaguegamefinder ===");
        var finderJson = await http.GetStringAsync(
            "https://stats.nba.com/stats/leaguegamefinder?LeagueID=00&Season=2024-25&SeasonType=Regular%20Season");

        var finder = JsonSerializer.Deserialize<LeagueGameFinderResponse>(finderJson, jsonOptions);
        var rs = finder?.ResultSets?.FirstOrDefault();
        if (rs != null)
        {
            int gameIdIdx = rs.Headers.IndexOf("GAME_ID");
            int dateIdx   = rs.Headers.IndexOf("GAME_DATE");
            int teamIdx   = rs.Headers.IndexOf("TEAM_ABBREVIATION");
            var sample    = rs.RowSet.First();
            Console.WriteLine($"  ResultSet: {rs.Name}, rows: {rs.RowSet.Count}");
            Console.WriteLine($"  Sample row → GameId={sample[gameIdIdx]}, Date={sample[dateIdx]}, Team={sample[teamIdx]}");
        }
        else Console.WriteLine("  FAILED — null result");

        await Task.Delay(5000);

        // --- Test 2: boxscoretraditionalv3 → BoxScoreTraditionalV3Response ---
        Console.WriteLine("\n=== boxscoretraditionalv3 ===");
        var tradJson = await http.GetStringAsync(
            "https://stats.nba.com/stats/boxscoretraditionalv3?GameID=0022400001");

        var trad = JsonSerializer.Deserialize<BoxScoreTraditionalV3Response>(tradJson, jsonOptions);
        var bs = trad?.BoxScoreTraditional;
        if (bs != null)
        {
            Console.WriteLine($"  GameId={bs.GameId}, HomeTeamId={bs.HomeTeamId}, AwayTeamId={bs.AwayTeamId}");
            var p = bs.HomeTeam.Players.First();
            var s = p.Statistics;
            Console.WriteLine($"  First home player: {p.FirstName} {p.FamilyName}");
            Console.WriteLine($"    minutes={s.Minutes} → decimal={s.MinutesDecimal():F2}");
            Console.WriteLine($"    pts={s.Points}, reb={s.ReboundsTotal}, ast={s.Assists}");
            Console.WriteLine($"    fgm={s.FieldGoalsMade}/{s.FieldGoalsAttempted}, +/-={s.PlusMinusPoints}");
        }
        else Console.WriteLine("  FAILED — null result");

        await Task.Delay(5000);

        // --- Test 3: boxscoreadvancedv3 → BoxScoreAdvancedV3Response ---
        Console.WriteLine("\n=== boxscoreadvancedv3 ===");
        var advJson = await http.GetStringAsync(
            "https://stats.nba.com/stats/boxscoreadvancedv3?GameID=0022400001");

        var adv = JsonSerializer.Deserialize<BoxScoreAdvancedV3Response>(advJson, jsonOptions);
        var abs = adv?.BoxScoreAdvanced;
        if (abs != null)
        {
            Console.WriteLine($"  GameId={abs.GameId}, HomeTeamId={abs.HomeTeamId}, AwayTeamId={abs.AwayTeamId}");
            var p = abs.HomeTeam.Players.First();
            var s = p.Statistics;
            Console.WriteLine($"  First home player personId={p.PersonId}");
            Console.WriteLine($"    minutes={s.Minutes} → decimal={s.MinutesDecimal():F2}");
            Console.WriteLine($"    offRtg={s.OffensiveRating}, defRtg={s.DefensiveRating}, netRtg={s.NetRating}");
            Console.WriteLine($"    TS%={s.TrueShootingPercentage:P1}, USG%={s.UsagePercentage:P1}, PIE={s.PIE:F3}");
        }
        else Console.WriteLine("  FAILED — null result");

        Console.WriteLine("\nAll model validation tests done.");
    }
}

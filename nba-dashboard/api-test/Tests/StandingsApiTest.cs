using System.Diagnostics;
using NbaDashboard.Infrastructure.NbaStats;

namespace ApiTest.Tests;

/// <summary>
/// Validates leaguestandingsv3 endpoint via curl — prints headers, row count,
/// and first few teams' standings data.
/// </summary>
public static class StandingsApiTest
{
    public static async Task RunAsync()
    {
        // Try current season first, fall back to previous
        var now = DateTime.UtcNow;
        int seasonYear = now.Month >= 10 ? now.Year : now.Year - 1;
        string[] seasons = { $"{seasonYear}-{(seasonYear + 1) % 100:D2}", $"{seasonYear - 1}-{seasonYear % 100:D2}" };

        foreach (var seasonStr in seasons)
        {
            Console.WriteLine($"\n--- Trying season {seasonStr} ---");
            var ok = await TrySeasonAsync(seasonStr);
            if (ok) break;
            await Task.Delay(5000);
        }
    }

    private static async Task<bool> TrySeasonAsync(string seasonStr)
    {
        var url = "https://stats.nba.com/stats/leaguestandingsv3"
            + $"?LeagueID=00&Season={seasonStr}&SeasonType=Regular%20Season";

        Console.WriteLine("=== " + url + " ===");
        try
        {
            var psi = new ProcessStartInfo("curl")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            psi.ArgumentList.Add("-s");
            psi.ArgumentList.Add("--compressed");
            psi.ArgumentList.Add("--max-time");
            psi.ArgumentList.Add("30");

            foreach (var (key, value) in NbaStatsHeaders.Default)
            {
                psi.ArgumentList.Add("-H");
                psi.ArgumentList.Add($"{key}: {value}");
            }
            psi.ArgumentList.Add(url);

            var process = Process.Start(psi)!;
            var json = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync();
                Console.WriteLine($"curl failed (exit {process.ExitCode}): {stderr}");
                return false;
            }

            Console.WriteLine("Response length: " + json.Length + " chars");
            Console.WriteLine(json.Substring(0, Math.Min(2000, json.Length)));

            var doc = System.Text.Json.JsonDocument.Parse(json);
            var resultSet = doc.RootElement.GetProperty("resultSets")[0];

            var headers = resultSet.GetProperty("headers");
            Console.WriteLine("\nHEADERS: " + headers.ToString());

            var rows = resultSet.GetProperty("rowSet");
            Console.WriteLine($"ROW COUNT: {rows.GetArrayLength()}");

            // Print header index for key columns
            var headerList = new List<string>();
            foreach (var h in headers.EnumerateArray())
                headerList.Add(h.GetString() ?? "");

            string[] keyCols = { "TeamID", "TeamCity", "TeamName", "WINS", "LOSSES",
                "WinPCT", "PlayoffRank", "HOME", "ROAD", "L10", "strCurrentStreak",
                "OffRating", "DefRating", "NetRating", "Pace", "Conference", "DivisionRank" };

            Console.WriteLine("\nKey column indices:");
            foreach (var col in keyCols)
            {
                var idx = headerList.IndexOf(col);
                Console.WriteLine($"  {col}: {(idx >= 0 ? idx.ToString() : "NOT FOUND")}");
            }

            // Print first 3 teams
            Console.WriteLine("\nFirst 3 teams:");
            int count = 0;
            foreach (var row in rows.EnumerateArray())
            {
                if (count++ >= 3) break;
                var teamCity = headerList.IndexOf("TeamCity") >= 0
                    ? row[headerList.IndexOf("TeamCity")].GetString() : "?";
                var teamName = headerList.IndexOf("TeamName") >= 0
                    ? row[headerList.IndexOf("TeamName")].GetString() : "?";
                var wins = headerList.IndexOf("WINS") >= 0
                    ? row[headerList.IndexOf("WINS")].ToString() : "?";
                var losses = headerList.IndexOf("LOSSES") >= 0
                    ? row[headerList.IndexOf("LOSSES")].ToString() : "?";
                var rank = headerList.IndexOf("PlayoffRank") >= 0
                    ? row[headerList.IndexOf("PlayoffRank")].ToString() : "?";
                Console.WriteLine($"  #{rank} {teamCity} {teamName}: {wins}-{losses}");
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Failed: " + ex.Message);
            return false;
        }
    }
}

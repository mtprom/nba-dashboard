using System.Diagnostics;
using NbaDashboard.Infrastructure.NbaStats;

namespace ApiTest.Tests;

/// <summary>
/// Validates leaguedashplayerstats endpoint via curl — prints headers and first 2000 chars
/// for both traditional (PerMode=PerGame) and advanced (MeasureType=Advanced) calls.
/// </summary>
public static class SeasonStatsApiTest
{
    public static async Task RunAsync()
    {
        var baseParams = "LeagueID=00&Season=2024-25&SeasonType=Regular%20Season&PerMode=PerGame"
            + "&DateFrom=&DateTo=&GameScope=&GameSegment=&LastNGames=0&Location="
            + "&Month=0&OpponentTeamID=0&Outcome=&PORound=0&PaceAdjust=N"
            + "&Period=0&PlayerExperience=&PlayerPosition=&PlusMinus=N&Rank=N"
            + "&SeasonSegment=&ShotClockRange=&StarterBench=&TeamID=0"
            + "&TwoWay=0&VsConference=&VsDivision=&Weight=";

        string[] urls =
        {
            // Traditional per-game stats
            $"https://stats.nba.com/stats/leaguedashplayerstats?{baseParams}&MeasureType=Base",
            // Advanced stats
            $"https://stats.nba.com/stats/leaguedashplayerstats?{baseParams}&MeasureType=Advanced",
        };

        foreach (var url in urls)
        {
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
                    continue;
                }

                Console.WriteLine(json.Substring(0, Math.Min(2000, json.Length)));

                // Parse headers for quick reference
                var doc = System.Text.Json.JsonDocument.Parse(json);
                var headers = doc.RootElement
                    .GetProperty("resultSets")[0]
                    .GetProperty("headers");
                Console.WriteLine("\nHEADERS: " + headers.ToString());

                var rowCount = doc.RootElement
                    .GetProperty("resultSets")[0]
                    .GetProperty("rowSet")
                    .GetArrayLength();
                Console.WriteLine($"ROW COUNT: {rowCount}");
                Console.WriteLine("\n");
                await Task.Delay(5000);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed: " + ex.Message);
            }
        }
    }
}

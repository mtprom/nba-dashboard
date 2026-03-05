using NbaDashboard.Infrastructure.NbaStats;

namespace ApiTest.Tests;

/// <summary>
/// Original test — prints raw JSON (first 2000 chars) for all 3 endpoints.
/// </summary>
public static class RawApiTest
{
    public static async Task RunAsync()
    {
        System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

        var handler = new HttpClientHandler();
        NbaStatsHeaders.ConfigureHandler(handler);

        using var client = new HttpClient(handler);
        foreach (var (key, value) in NbaStatsHeaders.Default)
            client.DefaultRequestHeaders.TryAddWithoutValidation(key, value);

        string[] urls =
        {
            "https://stats.nba.com/stats/leaguegamefinder?LeagueID=00&Season=2024-25&SeasonType=Regular%20Season",
            "https://stats.nba.com/stats/boxscoretraditionalv3?GameID=0022400001",
            "https://stats.nba.com/stats/boxscoreadvancedv3?GameID=0022400001"
        };

        foreach (var url in urls)
        {
            Console.WriteLine("=== " + url + " ===");
            try
            {
                var json = await client.GetStringAsync(url);
                Console.WriteLine(json.Substring(0, Math.Min(2000, json.Length)));
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

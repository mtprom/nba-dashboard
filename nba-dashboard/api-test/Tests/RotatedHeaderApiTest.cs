using System.Diagnostics;

namespace ApiTest.Tests;

/// <summary>
/// Integration test: defines multiple browser-fingerprint header profiles
/// and makes one real API call per profile to stats.nba.com via curl.
/// Reports which profiles succeed, which get blocked, and what the
/// HTTP status / body looks like on failure. Uses 8s delays between calls.
/// </summary>
public static class RotatedHeaderApiTest
{
    // ── Header profiles to test ──────────────────────────────────────────
    // Each profile mimics a different browser. Common headers (Origin,
    // Referer, Accept, etc.) are shared; only the fingerprint-specific
    // headers vary.

    private static readonly Dictionary<string, string> CommonHeaders = new()
    {
        ["Accept"]          = "application/json, text/plain, */*",
        ["Accept-Encoding"] = "gzip, deflate, br",
        ["Cache-Control"]   = "no-cache",
        ["Origin"]          = "https://www.nba.com",
        ["Pragma"]          = "no-cache",
        ["Referer"]         = "https://www.nba.com/",
        ["Sec-Fetch-Dest"]  = "empty",
        ["Sec-Fetch-Mode"]  = "cors",
        ["Sec-Fetch-Site"]  = "same-site",
    };

    private record HeaderProfile(string Name, Dictionary<string, string> Headers);

    private static readonly HeaderProfile[] Profiles =
    {
        new("Chrome 131 / Windows", new Dictionary<string, string>(CommonHeaders)
        {
            ["User-Agent"]         = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
                                   + "AppleWebKit/537.36 (KHTML, like Gecko) "
                                   + "Chrome/131.0.0.0 Safari/537.36",
            ["Accept-Language"]    = "en-US,en;q=0.9",
            ["Sec-Ch-Ua"]          = "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"",
            ["Sec-Ch-Ua-Mobile"]   = "?0",
            ["Sec-Ch-Ua-Platform"] = "\"Windows\"",
        }),

        new("Chrome 131 / macOS", new Dictionary<string, string>(CommonHeaders)
        {
            ["User-Agent"]         = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) "
                                   + "AppleWebKit/537.36 (KHTML, like Gecko) "
                                   + "Chrome/131.0.0.0 Safari/537.36",
            ["Accept-Language"]    = "en-US,en;q=0.9",
            ["Sec-Ch-Ua"]          = "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"",
            ["Sec-Ch-Ua-Mobile"]   = "?0",
            ["Sec-Ch-Ua-Platform"] = "\"macOS\"",
        }),

        new("Firefox 134 / Windows", new Dictionary<string, string>(CommonHeaders)
        {
            ["User-Agent"]      = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:134.0) "
                                + "Gecko/20100101 Firefox/134.0",
            ["Accept-Language"] = "en-US,en;q=0.5",
            // Firefox does not send Sec-Ch-Ua headers
        }),

        new("Safari 18.2 / macOS", new Dictionary<string, string>(CommonHeaders)
        {
            ["User-Agent"]      = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) "
                                + "AppleWebKit/605.1.15 (KHTML, like Gecko) "
                                + "Version/18.2 Safari/605.1.15",
            ["Accept-Language"] = "en-US,en;q=0.9",
            // Safari does not send Sec-Ch-Ua headers
        }),

        new("Chrome 131 / Linux", new Dictionary<string, string>(CommonHeaders)
        {
            ["User-Agent"]         = "Mozilla/5.0 (X11; Linux x86_64) "
                                   + "AppleWebKit/537.36 (KHTML, like Gecko) "
                                   + "Chrome/131.0.0.0 Safari/537.36",
            ["Accept-Language"]    = "en-US,en;q=0.9",
            ["Sec-Ch-Ua"]          = "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"",
            ["Sec-Ch-Ua-Mobile"]   = "?0",
            ["Sec-Ch-Ua-Platform"] = "\"Linux\"",
        }),
    };

    // ── Test runner ──────────────────────────────────────────────────────

    public static async Task RunAsync()
    {
        Console.WriteLine("=== Rotated Header API Test ===\n");
        Console.WriteLine($"Testing {Profiles.Length} header profiles against stats.nba.com");
        Console.WriteLine("Endpoint: leaguegamefinder (single day, minimal data)\n");

        int successes = 0;
        int failures = 0;

        var url = "https://stats.nba.com/stats/leaguegamefinder"
            + "?LeagueID=00&Season=2024-25&SeasonType=Regular%20Season"
            + "&DateFrom=2024-10-22&DateTo=2024-10-22";

        for (int i = 0; i < Profiles.Length; i++)
        {
            var profile = Profiles[i];
            Console.Write($"  [{i + 1}/{Profiles.Length}] {profile.Name,-25} ");

            try
            {
                var (httpCode, body, exitCode) = await CurlAsync(url, profile.Headers);

                if (httpCode == 200 && body.TrimStart().StartsWith('{'))
                {
                    Console.WriteLine($"✓ HTTP {httpCode}  ({body.Length:N0} bytes)");
                    successes++;
                }
                else if (exitCode != 0)
                {
                    Console.WriteLine($"✗ curl exit {exitCode}");
                    failures++;
                }
                else
                {
                    var preview = body.Length > 100 ? body[..100] + "..." : body;
                    var blocked = IsAkamaiBlock(body) ? " [Akamai block]" : "";
                    Console.WriteLine($"✗ HTTP {httpCode}{blocked}  body: {preview}");
                    failures++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✗ ERROR: {ex.Message}");
                failures++;
            }

            // 8s delay between requests
            if (i < Profiles.Length - 1)
            {
                Console.WriteLine($"       waiting 8s...");
                await Task.Delay(8000);
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Results: {successes} passed, {failures} failed out of {Profiles.Length}");
        Console.WriteLine();

        if (successes == 0)
            Console.WriteLine("CRITICAL: No header profile succeeded — API may be blocking this IP entirely.");
        else if (failures > 0)
            Console.WriteLine("WARNING: Some profiles were blocked — see output above for details.");
        else
            Console.WriteLine("All profiles working — safe to use for rotation.");
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static async Task<(int httpCode, string body, int exitCode)> CurlAsync(
        string url, Dictionary<string, string> headers)
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
        psi.ArgumentList.Add("-w");
        psi.ArgumentList.Add("\n%{http_code}");

        foreach (var (key, value) in headers)
        {
            psi.ArgumentList.Add("-H");
            psi.ArgumentList.Add($"{key}: {value}");
        }
        psi.ArgumentList.Add(url);

        var process = Process.Start(psi)!;
        var rawOutput = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        // Parse HTTP status code from last line (written by -w)
        var lastNewline = rawOutput.LastIndexOf('\n');
        var httpCodeStr = lastNewline >= 0 ? rawOutput[(lastNewline + 1)..].Trim() : "0";
        int.TryParse(httpCodeStr, out var httpCode);
        var body = lastNewline >= 0 ? rawOutput[..lastNewline] : rawOutput;

        return (httpCode, body, process.ExitCode);
    }

    private static bool IsAkamaiBlock(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;
        var trimmed = body.TrimStart();
        return trimmed.StartsWith("<!")
            || trimmed.StartsWith("<html", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("Access Denied", StringComparison.OrdinalIgnoreCase)
            || trimmed.Contains("Reference #", StringComparison.OrdinalIgnoreCase);
    }
}

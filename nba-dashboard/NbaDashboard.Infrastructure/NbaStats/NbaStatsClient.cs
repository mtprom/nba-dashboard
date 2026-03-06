using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NbaDashboard.Infrastructure.NbaStats;

public class NbaStatsClient
{
    private readonly ILogger<NbaStatsClient> _logger;
    private static readonly SemaphoreSlim _throttle = new(1, 1);
    private static readonly Random _rng = new();
    private const int MinDelayMs = 4000;
    private const int MaxDelayMs = 6000;
    private const int CooldownIntervalMs = 5 * 60 * 1000; // 5 minutes
    private const int CooldownDurationMs = 30_000;         // 30 seconds
    private static readonly Stopwatch _sessionTimer = Stopwatch.StartNew();
    private const string BaseUrl = "https://stats.nba.com/stats";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public NbaStatsClient(HttpClient httpClient, ILogger<NbaStatsClient> logger)
    {
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string endpoint, Dictionary<string, string>? parameters = null,
        CancellationToken ct = default)
    {
        await _throttle.WaitAsync(ct);

        try
        {
            var url = $"{BaseUrl}/{endpoint}";
            if (parameters is { Count: > 0 })
            {
                var qs = string.Join("&", parameters
                    .Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
                url = $"{url}?{qs}";
            }

            _logger.LogInformation("GET {Url}", url);

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
            var json = await process.StandardOutput.ReadToEndAsync(ct);
            await process.WaitForExitAsync(ct);

            if (process.ExitCode != 0)
            {
                var stderr = await process.StandardError.ReadToEndAsync(ct);
                throw new HttpRequestException($"curl failed (exit {process.ExitCode}): {stderr}");
            }

            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Request failed for {Endpoint}", endpoint);
            throw;
        }
        finally
        {
            if (_sessionTimer.ElapsedMilliseconds >= CooldownIntervalMs)
            {
                _logger.LogInformation("Cooldown: pausing {Seconds}s", CooldownDurationMs / 1000);
                await Task.Delay(CooldownDurationMs, ct);
                _sessionTimer.Restart();
            }
            await Task.Delay(_rng.Next(MinDelayMs, MaxDelayMs + 1), ct);
            _throttle.Release();
        }
    }
}

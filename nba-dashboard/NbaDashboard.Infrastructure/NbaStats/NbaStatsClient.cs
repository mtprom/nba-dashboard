using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NbaDashboard.Infrastructure.NbaStats;

public class NbaStatsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NbaStatsClient> _logger;
    private static readonly SemaphoreSlim _throttle = new(1, 1);
    private static readonly TimeSpan _delay = TimeSpan.FromMilliseconds(5000);
    private const string BaseUrl = "https://stats.nba.com/stats";

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public NbaStatsClient(HttpClient httpClient, ILogger<NbaStatsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        foreach (var (key, value) in NbaStatsHeaders.Default)
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
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

            var response = await _httpClient.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            return JsonSerializer.Deserialize<T>(json, _jsonOptions);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Request failed for {Endpoint}", endpoint);
            throw;
        }
        finally
        {
            await Task.Delay(_delay, ct);
            _throttle.Release();
        }
    }
}

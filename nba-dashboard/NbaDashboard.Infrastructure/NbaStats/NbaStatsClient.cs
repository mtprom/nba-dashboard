using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace NbaDashboard.Infrastructure.NbaStats;

public class NbaStatsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NbaStatsClient> _logger;
    private static readonly SemaphoreSlim _throttle = new(1, 1);
    private static readonly TimeSpan _delay = TimeSpan.FromMilliseconds(5000);

    public NbaStatsClient(HttpClient httpClient, ILogger<NbaStatsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        foreach (var (key, value) in NbaStatsHeaders.Default)
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(key, value);
    }

    public async Task<T?> GetAsync<T>(string endpoint, CancellationToken ct = default)
    {
        await _throttle.WaitAsync(ct);

        try
        {
            _logger.LogInformation("GET {Endpoint}", endpoint);

            var response = await _httpClient.GetAsync(endpoint, ct);
            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct
            );
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
